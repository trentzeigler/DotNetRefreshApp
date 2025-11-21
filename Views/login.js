const loginForm = document.getElementById('loginForm');
const signupForm = document.getElementById('signupForm');
const showSignupLink = document.getElementById('showSignup');
const showLoginLink = document.getElementById('showLogin');
const errorMessage = document.getElementById('errorMessage');
const successMessage = document.getElementById('successMessage');

// Check if already authenticated
if (sessionStorage.getItem('authToken')) {
    window.location.href = '/ai/conversation';
}

// Check for email verification success
const urlParams = new URLSearchParams(window.location.search);
if (urlParams.get('verified') === 'true') {
    showSuccess('Email verified successfully! Please sign in.');
}

// Toggle between login and signup forms
showSignupLink.addEventListener('click', (e) => {
    e.preventDefault();
    loginForm.style.display = 'none';
    signupForm.style.display = 'block';
    clearMessages();
});

showLoginLink.addEventListener('click', (e) => {
    e.preventDefault();
    signupForm.style.display = 'none';
    loginForm.style.display = 'block';
    clearMessages();
});

// Login form submission
loginForm.addEventListener('submit', async (e) => {
    e.preventDefault();
    clearMessages();

    const email = document.getElementById('loginEmail').value.trim();
    const password = document.getElementById('loginPassword').value;

    const submitButton = loginForm.querySelector('button[type="submit"]');
    submitButton.disabled = true;
    submitButton.textContent = 'Signing in...';

    try {
        const response = await fetch('/api/auth/login', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ email, password })
        });

        const data = await response.json();

        if (!response.ok) {
            throw new Error(data.error || 'Login failed');
        }

        // Store auth data in session storage
        sessionStorage.setItem('authToken', data.token);
        sessionStorage.setItem('userId', data.userId);
        sessionStorage.setItem('userEmail', data.email);
        sessionStorage.setItem('userName', data.name);
        sessionStorage.setItem('emailVerified', data.emailVerified);

        // Redirect to chat
        window.location.href = '/ai/conversation';
    } catch (error) {
        showError(error.message);
        submitButton.disabled = false;
        submitButton.textContent = 'Sign In';
    }
});

// Signup form submission
signupForm.addEventListener('submit', async (e) => {
    e.preventDefault();
    clearMessages();

    const name = document.getElementById('signupName').value.trim();
    const email = document.getElementById('signupEmail').value.trim();
    const password = document.getElementById('signupPassword').value;

    // Frontend validation
    if (!validatePassword(password)) {
        return;
    }

    const submitButton = signupForm.querySelector('button[type="submit"]');
    submitButton.disabled = true;
    submitButton.textContent = 'Creating account...';

    try {
        const response = await fetch('/api/auth/signup', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ name, email, password })
        });

        const data = await response.json();

        if (!response.ok) {
            throw new Error(data.error || 'Signup failed');
        }

        // Store auth data in session storage
        sessionStorage.setItem('authToken', data.token);
        sessionStorage.setItem('userId', data.userId);
        sessionStorage.setItem('userEmail', data.email);
        sessionStorage.setItem('userName', data.name);
        sessionStorage.setItem('emailVerified', data.emailVerified);

        // Show success message about email verification
        showSuccess('Account created! Please check your email to verify your address.');

        // Redirect to chat after a brief delay
        setTimeout(() => {
            window.location.href = '/ai/conversation';
        }, 2000);
    } catch (error) {
        showError(error.message);
        submitButton.disabled = false;
        submitButton.textContent = 'Create Account';
    }
});

/**
 * Validate password meets requirements
 */
function validatePassword(password) {
    if (password.length < 10) {
        showError('Password must be at least 10 characters long');
        return false;
    }

    // Check for special character
    const specialCharRegex = /[!@#$%^&*(),.?":{}|<>]/;
    if (!specialCharRegex.test(password)) {
        showError('Password must contain at least one special character (!@#$%^&*...)');
        return false;
    }

    return true;
}

/**
 * Show error message
 */
function showError(message) {
    errorMessage.textContent = message;
    errorMessage.classList.add('active');
    successMessage.classList.remove('active');
}

/**
 * Show success message
 */
function showSuccess(message) {
    successMessage.textContent = message;
    successMessage.classList.add('active');
    errorMessage.classList.remove('active');
}

/**
 * Clear all messages
 */
function clearMessages() {
    errorMessage.classList.remove('active');
    successMessage.classList.remove('active');
}
