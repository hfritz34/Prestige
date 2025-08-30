using Microsoft.Data.SqlClient;
using System;
using System.IO;

class Program
{
    private static readonly string ConnectionString = "Server=tcp:prestigeserver.database.windows.net,1433;Initial Catalog=prestigesqldb;Persist Security Info=False;User ID=Prestige;Password=offline!Reboun002392d;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";
    
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== PRESTIGE DUMMY USER CREATOR ===");
        Console.WriteLine();
        
        try
        {
            // Read the SQL script
            string scriptPath = Path.Combine("..", "InsertDummyUserData.sql");
            if (!File.Exists(scriptPath))
            {
                Console.WriteLine($"Error: SQL script not found at {scriptPath}");
                Console.WriteLine("Make sure InsertDummyUserData.sql is in the Scripts folder.");
                return;
            }
            
            string sqlScript = await File.ReadAllTextAsync(scriptPath);
            Console.WriteLine("✓ SQL script loaded successfully");
            
            // Execute the script
            using var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync();
            Console.WriteLine("✓ Connected to database");
            
            // First, let's check what tables exist
            Console.WriteLine("Checking existing tables...");
            using var checkTablesCmd = new SqlCommand("SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE' ORDER BY TABLE_NAME", connection);
            using var reader = await checkTablesCmd.ExecuteReaderAsync();
            
            var tables = new List<string>();
            while (await reader.ReadAsync())
            {
                var tableName = reader.GetString(0);
                tables.Add(tableName);
                Console.WriteLine($"  - {tableName}");
            }
            reader.Close();
            
            Console.WriteLine();
            
            // Split the script into individual statements (simple approach)
            var statements = sqlScript.Split(new[] { "GO" }, StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var statement in statements)
            {
                var cleanStatement = statement.Trim();
                if (string.IsNullOrEmpty(cleanStatement)) continue;
                
                using var command = new SqlCommand(cleanStatement, connection);
                command.CommandTimeout = 60; // 60 seconds timeout
                
                try
                {
                    var result = await command.ExecuteNonQueryAsync();
                    Console.WriteLine($"✓ Executed SQL block");
                }
                catch (Exception sqlEx)
                {
                    Console.WriteLine($"❌ SQL Error in statement: {sqlEx.Message}");
                    // Continue with the next statement
                }
            }
            
            Console.WriteLine();
            Console.WriteLine("=== DUMMY USER CREATION COMPLETE ===");
            Console.WriteLine();
            Console.WriteLine("Dummy User Details:");
            Console.WriteLine("- User ID: dummy_spotify_user_12345");
            Console.WriteLine("- Name: Test Friend");
            Console.WriteLine("- Nickname: TestBuddy");
            Console.WriteLine("- Email: testfriend@example.com");
            Console.WriteLine();
            Console.WriteLine("Test Data Created:");
            Console.WriteLine("- Artist: OsamaSon (2 hours listening, rating 10.0)");
            Console.WriteLine("- Album: Flex Musix (2 hours listening, rating 10.0)");
            Console.WriteLine("- Track: Kills (1 hour listening, position #1, rating 10.0)");
            Console.WriteLine("- Track: All Star (1 hour listening, position #2, rating 9.5)");
            Console.WriteLine();
            Console.WriteLine("Next Steps:");
            Console.WriteLine("1. Open your mobile app");
            Console.WriteLine("2. Search for 'TestBuddy' or use user ID 'dummy_spotify_user_12345'");
            Console.WriteLine("3. Add as friend to test all friends features");
            Console.WriteLine();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error: {ex.Message}");
            Console.WriteLine();
            Console.WriteLine("Stack trace:");
            Console.WriteLine(ex.StackTrace);
        }
        
        Console.WriteLine("Script execution completed.");
    }
}
