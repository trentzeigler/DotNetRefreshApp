using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DotNetRefreshApp.Data;
using DotNetRefreshApp.Models;
using DotNetRefreshApp.Models.Auth;
using DotNetRefreshApp.Services;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace DotNetRefreshApp.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IJwtService _jwtService;
        private readonly IEmailService _emailService;
        private readonly ILogger<AuthController> _logger;
        private readonly IWebHostEnvironment _env;

        public AuthController(
            AppDbContext context,
            IPasswordHasher passwordHasher,
            IJwtService jwtService,
            IEmailService emailService,
            ILogger<AuthController> logger,
            IWebHostEnvironment env)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _jwtService = jwtService;
            _emailService = emailService;
            _logger = logger;
            _env = env;
        }

        /// <summary>
        /// POST /api/auth/signup - Register a new user
        /// </summary>
        [HttpPost("signup")]
        public async Task<IActionResult> Signup([FromBody] SignupRequest request)
        {
            try
            {
                // Validate password requirements
                if (!IsValidPassword(request.Password, out string passwordError))
                {
                    return BadRequest(new { error = passwordError });
                }

                // Check if email already exists
                var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
                if (existingUser != null)
                {
                    return BadRequest(new { error = "Email already registered" });
                }

                // Generate email verification token
                var verificationToken = Guid.NewGuid().ToString();

                // Create new user
                var user = new User
                {
                    Name = request.Name,
                    Email = request.Email,
                    PasswordHash = _passwordHasher.HashPassword(request.Password),
                    Role = "User",
                    EmailVerified = false,
                    EmailVerificationToken = verificationToken,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Send verification email
                await SendVerificationEmail(user.Email, user.Name, verificationToken);

                // Generate JWT token
                var token = _jwtService.GenerateToken(user);

                return Ok(new AuthResponse
                {
                    Token = token,
                    UserId = user.Id,
                    Email = user.Email,
                    Name = user.Name,
                    EmailVerified = user.EmailVerified
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during signup");
                return StatusCode(500, new { error = "An error occurred during signup" });
            }
        }

        /// <summary>
        /// POST /api/auth/login - Authenticate user
        /// </summary>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                // Find user by email
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
                if (user == null)
                {
                    return Unauthorized(new { error = "Invalid email or password" });
                }

                // Verify password
                if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
                {
                    return Unauthorized(new { error = "Invalid email or password" });
                }

                // Generate JWT token
                var token = _jwtService.GenerateToken(user);

                return Ok(new AuthResponse
                {
                    Token = token,
                    UserId = user.Id,
                    Email = user.Email,
                    Name = user.Name,
                    EmailVerified = user.EmailVerified
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login");
                return StatusCode(500, new { error = "An error occurred during login" });
            }
        }

        /// <summary>
        /// GET /api/auth/verify-email?token={token} - Verify user's email address
        /// </summary>
        [HttpGet("verify-email")]
        public async Task<IActionResult> VerifyEmail([FromQuery] string token)
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.EmailVerificationToken == token);
                
                if (user == null)
                {
                    return BadRequest(new { error = "Invalid verification token" });
                }

                user.EmailVerified = true;
                user.EmailVerificationToken = null;
                await _context.SaveChangesAsync();

                // Redirect to login page with success message
                return Redirect("/api/auth/login?verified=true");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during email verification");
                return StatusCode(500, new { error = "An error occurred during verification" });
            }
        }

        /// <summary>
        /// GET /api/auth/me - Get current user info from JWT token
        /// </summary>
        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> GetCurrentUser()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var user = await _context.Users.FindAsync(userId);

                if (user == null)
                {
                    return NotFound(new { error = "User not found" });
                }

                return Ok(new
                {
                    userId = user.Id,
                    email = user.Email,
                    name = user.Name,
                    emailVerified = user.EmailVerified,
                    role = user.Role
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current user");
                return StatusCode(500, new { error = "An error occurred" });
            }
        }

        /// <summary>
        /// GET /api/auth/login - Serve login page
        /// </summary>
        [HttpGet("login")]
        public async Task<IActionResult> LoginPage()
        {
            var htmlPath = Path.Combine(_env.ContentRootPath, "Views", "login.html");
            
            if (!System.IO.File.Exists(htmlPath))
            {
                return NotFound("Login page not found");
            }

            var html = await System.IO.File.ReadAllTextAsync(htmlPath);
            return Content(html, "text/html");
        }

        /// <summary>
        /// GET /api/auth/login.css - Serve login CSS
        /// </summary>
        [HttpGet("login.css")]
        public async Task<IActionResult> LoginCss()
        {
            var cssPath = Path.Combine(_env.ContentRootPath, "Views", "login.css");
            
            if (!System.IO.File.Exists(cssPath))
            {
                return NotFound("CSS not found");
            }

            var css = await System.IO.File.ReadAllTextAsync(cssPath);
            return Content(css, "text/css");
        }

        /// <summary>
        /// GET /api/auth/login.js - Serve login JavaScript
        /// </summary>
        [HttpGet("login.js")]
        public async Task<IActionResult> LoginJs()
        {
            var jsPath = Path.Combine(_env.ContentRootPath, "Views", "login.js");
            
            if (!System.IO.File.Exists(jsPath))
            {
                return NotFound("JavaScript not found");
            }

            var js = await System.IO.File.ReadAllTextAsync(jsPath);
            return Content(js, "text/javascript");
        }

        /// <summary>
        /// Validate password meets requirements
        /// </summary>
        private bool IsValidPassword(string password, out string error)
        {
            error = string.Empty;

            if (password.Length < 10)
            {
                error = "Password must be at least 10 characters long";
                return false;
            }

            // Check for at least one special character
            if (!Regex.IsMatch(password, @"[!@#$%^&*(),.?""':{}|<>]"))
            {
                error = "Password must contain at least one special character";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Send email verification email
        /// </summary>
        private async Task SendVerificationEmail(string email, string name, string token)
        {
            try
            {
                var verificationLink = $"{Request.Scheme}://{Request.Host}/api/auth/verify-email?token={token}";
                
                var emailDraft = new EmailDraft
                {
                    To = email,
                    From = "noreply@example.com", // TODO: Set your sender email
                    Subject = "Verify Your Email Address",
                    Body = $"Hi {name},\n\nPlease verify your email address by clicking the link below:\n\n{verificationLink}\n\nIf you didn't create an account, you can ignore this email.\n\nThanks!",
                    IsHtml = false
                };

                await _emailService.SendEmailAsync(emailDraft);
                _logger.LogInformation($"Verification email sent to {email}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send verification email to {email}");
                // Don't throw - email failure shouldn't block signup
            }
        }
    }
}
