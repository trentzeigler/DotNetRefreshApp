-- Mark InitialCreate migration as applied
-- Run this manually in Azure Data Studio or Azure Portal Query Editor

INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
VALUES ('20251120152959_InitialCreate', '7.0.10');

-- Then run the update command:
-- dotnet-ef database update --connection "YOUR_CONNECTION_STRING"
