using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;

namespace migrator
{
    public static class Migrate
    {
        public static void Run(string connectionString)
        {
            var localMigrations = GetLocalMigrations();
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                var pendingMigrations = GetPendingMigrations(connection, localMigrations);
                MigrateDatabase(pendingMigrations, connection);
                Console.WriteLine("Done.");
            }
        }

        private static void MigrateDatabase(IEnumerable<Migration> pendingMigrations, SqlConnection connection)
        {
            try
            {
                foreach (var migration in pendingMigrations)
                {
                    var command = connection.CreateCommand();
                    var transaction = connection.BeginTransaction(migration.Name);
                    command.Connection = connection;
                    command.Transaction = transaction;
                    try
                    {
                        command.CommandText = migration.Query;
                        command.ExecuteNonQuery();
                        command.CommandText = $"INSERT INTO Migrations (Name) VALUES ('{migration.Name}')";
                        command.ExecuteNonQuery();
                        transaction.Commit();
                        Console.WriteLine($"Added {migration.Name} to migrations table");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Commit Exception Type: {0}", ex.GetType());
                        Console.WriteLine("  Message: {0}", ex.Message);

                        try
                        {
                            transaction.Rollback();
                        }
                        catch (Exception ex2)
                        {
                            Console.WriteLine("Rollback Exception Type: {0}", ex2.GetType());
                            Console.WriteLine("  Message: {0}", ex2.Message);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                throw;
            }
        }

        private static List<Migration> GetPendingMigrations(SqlConnection connection, IEnumerable<Migration> allMigrations)
        {
            var list = new List<string>();

            try
            {
                using (var command = new SqlCommand("SELECT * FROM Migrations", connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                var t = reader.GetString(0);
                                list.Add(t);
                            }
                        }
                    }
                }

            }
            catch (System.Exception)
            {
            }

            var pendingMigrations = allMigrations.Where(m => list.All(l => l != m.Name)).ToList();
            Console.WriteLine($"There are {pendingMigrations.Count()} pending migrations");
            return pendingMigrations.OrderBy(p => p.Number).ToList();
        }

        private static IEnumerable<Migration> GetLocalMigrations()
        {
            var migrations = new List<Migration>();

            string migrationsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Migrations");
            System.Console.WriteLine($"Looking for local migrations here: {migrationsPath}");
            var migrationsFiles = Directory.GetFiles(migrationsPath);
            foreach (var migrationsFile in migrationsFiles)
            {
                var filename = Path.GetFileName(migrationsFile);
                var file = File.ReadAllText(migrationsFile);
                var nameAsArray = filename.Split('-');
                var success = int.TryParse(nameAsArray[0], out var migrationNumber);
                if (success)
                {
                    if (migrations.Any(m => m.Number == migrationNumber))
                    {
                        throw new ArgumentException(
                            $"Det finnes to migrasjoner med nummer {migrationNumber}. Slett nyeste");
                    }

                    migrations.Add(new Migration(filename, file, migrationNumber));
                }
                else
                {
                    throw new FormatException($"Migrasjonsfil må begynne med et nummer. Det gjør ikke {filename}");
                }
            }


            var distinctMigrationsNames = migrations.Select(m => m.Name).Distinct();
            if (distinctMigrationsNames.Count() != migrations.Count())
            {
                throw new Exception("Du har to migrasjoner med samme navn");
            }

            foreach (var migration in migrations.OrderBy(p => p.Number))
            {
                Console.WriteLine($"Fant migrasjon: {migration.Name}");
            }

            return migrations;
        }
    }
}
