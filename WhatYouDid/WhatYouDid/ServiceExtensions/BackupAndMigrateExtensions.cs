using Microsoft.EntityFrameworkCore;
using WhatYouDid.Data;

namespace WhatYouDid.ServiceExtensions;

public static class BackupAndMigrateExtensions
{
    public static void BackupAndMigrateSqlDatabase(this WebApplication app)
    {
        // Migrate the production server, with a pre-migration backup.
        var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var hasPendingMigrations = db.Database.GetPendingMigrations().Any();
        if (hasPendingMigrations)
        {
            var backupPath = app.Configuration["DatabaseBackupPath"]
                ?? throw new InvalidOperationException(
                    "DatabaseBackupPath is not configured. Add it to appsettings.json.");

            if (!Directory.Exists(backupPath))
                throw new InvalidOperationException($"Backup directory does not exist: {backupPath}");

            var connection = db.Database.GetDbConnection();
            var databaseName = connection.Database;
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

            static string MigName(string? migrationId) =>
                migrationId is null ? "init"
                : migrationId.Contains('_') ? migrationId[(migrationId.IndexOf('_') + 1)..] : migrationId;

            var currMig = MigName(db.Database.GetAppliedMigrations().LastOrDefault());
            var tgtMig = MigName(db.Database.GetPendingMigrations().Last());
            var backupFile = Path.Combine(backupPath, $"{databaseName}_{timestamp}_curr-{currMig}_to-{tgtMig}.bak");

            connection.Open();
            try
            {
                using var cmd = connection.CreateCommand();
                cmd.CommandText = $"BACKUP DATABASE [{databaseName}] TO DISK = @backupFile WITH FORMAT, INIT"; //, COMPRESSION (when using full sql)
                cmd.CommandTimeout = 300; // 5 minutes
                var param = cmd.CreateParameter();
                param.ParameterName = "@backupFile";
                param.Value = backupFile;
                cmd.Parameters.Add(param);
                cmd.ExecuteNonQuery();
            }
            finally
            {
                connection.Close();
            }

            Console.WriteLine($"[Backup] Completed before migration: {backupFile}");
            
            db.Database.Migrate();
        }
    }
}