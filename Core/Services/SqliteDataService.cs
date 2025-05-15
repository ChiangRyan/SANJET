using Microsoft.Data.Sqlite;
using SANJET.SANJET.Core.Models;
using System.Diagnostics;

namespace SANJET.SANJET.Core.Services
{
    public class SqliteDataService
    {
        private string _dbPath;
        private bool _isInitialized;
        private bool _shouldInsertTestData = false;

        public SqliteDataService(bool insertTestData = false)
        {
            _dbPath = "SJ_data.db";
            _isInitialized = false;
            _shouldInsertTestData = insertTestData;
            try
            {
                InitializeDatabase();
                if (_shouldInsertTestData)
                {
                    InsertTestDataIfEmpty();
                }
                Debug.WriteLine($"SqliteDataService initialized with database path: {_dbPath}, InsertTestData: {_shouldInsertTestData}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SqliteDataService initialization failed: {ex.Message}\nStackTrace: {ex.StackTrace}");
                throw;
            }
        }

        public bool IsPathAccessible(string path)
        {
            try
            {
                Debug.WriteLine($"Checking path accessibility: {path}");

                // 創建一個檢查路徑的任務
                var pathCheckTask = Task.Run(() =>
                {
                    try
                    {
                        // 嘗試訪問目錄並檢查檔案是否存在
                        string directory = System.IO.Path.GetDirectoryName(path);
                        if (!System.IO.Directory.Exists(directory))
                        {
                            Debug.WriteLine($"Directory does not exist: {directory}");
                            return false;
                        }
                        return System.IO.File.Exists(path);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Path check operation failed: {ex.Message}");
                        return false;
                    }
                });

                // 創建一個計時任務
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(2));

                // 等待任一任務完成
                var completedTask = Task.WhenAny(pathCheckTask, timeoutTask).Result;

                // 如果完成的是路徑檢查任務，返回其結果
                if (completedTask == pathCheckTask)
                {
                    bool result = pathCheckTask.Result;
                    Debug.WriteLine($"Path check completed with result: {result}");
                    return result;
                }
                // 如果是計時任務先完成，表示超時
                else
                {
                    Debug.WriteLine($"Path check timed out after 2 seconds for path: {path}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"IsPathAccessible failed for {path}: {ex.Message}\nStackTrace: {ex.StackTrace}");
                return false;
            }
        }

        public void SetDatabasePath(string path)
        {
            if (_dbPath != path) // 僅在路徑改變時重新初始化
            {
                _dbPath = path;
                _isInitialized = false;
                Debug.WriteLine($"Database path set to: {_dbPath}");
                InitializeDatabase(); // 重新初始化資料庫
                if (_shouldInsertTestData)
                {
                    InsertTestDataIfEmpty();
                }
            }
        }

        private void InitializeDatabase()
        {
            if (_isInitialized) return;

            try
            {
                Debug.WriteLine($"Initializing database at: {_dbPath}");
                using (var connection = new SqliteConnection($"Data Source={_dbPath}"))
                {
                    connection.Open();
                    var command = connection.CreateCommand();

                    // 創建 Users 表
                    command.CommandText = @"
                        CREATE TABLE IF NOT EXISTS Users (
                            Username TEXT PRIMARY KEY,
                            Password TEXT NOT NULL,
                            Permissions TEXT
                        )";
                    command.ExecuteNonQuery();
                    //command.Parameters.Clear(); // <--- 清除參數

                    // 創建 DeviceData 表
                    command.CommandText = @"
                        CREATE TABLE IF NOT EXISTS DeviceData (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            Name TEXT NOT NULL,
                            IpAddress TEXT NOT NULL,
                            SlaveId INTEGER NOT NULL,
                            IsOperational INTEGER NOT NULL,
                            RunCount INTEGER NOT NULL,
                            Timestamp TEXT NOT NULL
                        )";
                    command.ExecuteNonQuery();

                    // 為每個設備創建單獨的記錄表
                    for (int i = 1; i <= 12; i++)
                    {
                        command.CommandText = $@"
                            CREATE TABLE IF NOT EXISTS DeviceRecords_{i} (
                                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                DeviceName TEXT NOT NULL,
                                Username TEXT NOT NULL,
                                RunCount INTEGER NOT NULL,
                                Content TEXT NOT NULL,
                                Timestamp TEXT NOT NULL
                            )";
                        command.ExecuteNonQuery();
                    }

                    // 檢查並添加 Timestamp 欄位（如果不存在）
                    bool hasTimestamp = false;
                    command = connection.CreateCommand();
                    command.CommandText = "PRAGMA table_info(DeviceData)";
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (reader["name"].ToString() == "Timestamp")
                            {
                                hasTimestamp = true;
                                break;
                            }
                        }
                    }

                    if (!hasTimestamp)
                    {
                        command = connection.CreateCommand();
                        command.CommandText = "ALTER TABLE DeviceData ADD COLUMN Timestamp TEXT";
                        command.ExecuteNonQuery();
                        Debug.WriteLine("Added Timestamp column to DeviceData table.");
                    }

                    _isInitialized = true;
                    Debug.WriteLine("Database initialized successfully.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"InitializeDatabase failed: {ex.Message}\nStackTrace: {ex.StackTrace}");
                throw;
            }
        }

        private void InsertTestDataIfEmpty()
        {
            try
            {
                using (var connection = new SqliteConnection($"Data Source={_dbPath}"))
                {
                    connection.Open();
                    var command = connection.CreateCommand();
                    command.CommandText = "SELECT COUNT(*) FROM Users";
                    int userCount = Convert.ToInt32(command.ExecuteScalar());
                    Debug.WriteLine($"Users table has {userCount} records.");

                    if (userCount == 0)
                    {
                        command.CommandText = @"
                            INSERT INTO Users (Username, Password, Permissions)
                            VALUES (@username, @password, @permissions)";

                        command.Parameters.AddWithValue("@username", "administrator");
                        command.Parameters.AddWithValue("@password", "sanjet25653819");
                        command.Parameters.
                            AddWithValue("@permissions","ViewHome,ViewManualOperation,ViewMonitor,ViewWarning,ViewSettings,ControlDevice,All");
                        command.ExecuteNonQuery();
                        command.Parameters.Clear(); // <--- 清除參數

                        command.Parameters.AddWithValue("@username", "admin");
                        command.Parameters.AddWithValue("@password", "0000");
                        command.Parameters.
                            AddWithValue("@permissions", "ViewHome,ControlDevice");
                        command.ExecuteNonQuery();
                        command.Parameters.Clear(); // <--- 清除參數

                        command.Parameters.Clear();
                        command.Parameters.AddWithValue("@username", "user");
                        command.Parameters.AddWithValue("@password", "0000");
                        command.Parameters.AddWithValue("@permissions", "ViewHome");
                        command.ExecuteNonQuery();

                        Debug.WriteLine("Test data inserted successfully because Users table was empty.");
                    }
                    else
                    {
                        Debug.WriteLine("Skipped inserting test data because Users table is not empty.");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"InsertTestDataIfEmpty failed: {ex.Message}\nStackTrace: {ex.StackTrace}");
                throw;
            }
        }

        public User GetUserWithPermissions(string username, string password)
        {
            try
            {
                Debug.WriteLine($"Querying database for user: {username}");
                using (var connection = new SqliteConnection($"Data Source={_dbPath}"))
                {
                    connection.Open();
                    var command = connection.CreateCommand();
                    command.CommandText = "SELECT * FROM Users WHERE Username = @username AND Password = @password";
                    command.Parameters.AddWithValue("@username", username);
                    command.Parameters.AddWithValue("@password", password);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var user = new User
                            {
                                Username = reader["Username"].ToString(),
                                Password = reader["Password"].ToString(),
                                Permissions = reader["Permissions"]?.ToString()?.Split(',')?.ToList() ?? new List<string>()
                            };
                            Debug.WriteLine($"User found: {user.Username}, Permissions: {string.Join(",", user.Permissions)}");
                            return user;
                        }
                        Debug.WriteLine($"No user found for username: {username}");
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GetUserWithPermissions failed: {ex.Message}\nStackTrace: {ex.StackTrace}");
                throw;
            }
        }

        public List<DeviceData> GetDeviceData()
        {
            var deviceDataList = new List<DeviceData>();
            try
            {
                Debug.WriteLine("Querying all device data.");
                using (var connection = new SqliteConnection($"Data Source={_dbPath}"))
                {
                    connection.Open();
                    var command = connection.CreateCommand();
                    command.CommandText = "SELECT * FROM DeviceData";
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            deviceDataList.Add(new DeviceData
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                Name = reader["Name"].ToString(),
                                IpAddress = reader["IpAddress"].ToString(),
                                SlaveId = Convert.ToInt32(reader["SlaveId"]),
                                IsOperational = Convert.ToBoolean(reader["IsOperational"]),
                                RunCount = Convert.ToInt32(reader["RunCount"]),
                                // 讀取時
                                Timestamp = reader.IsDBNull(reader.GetOrdinal("Timestamp")) 
                                ? DateTime.MinValue
                                : DateTime.ParseExact(reader["Timestamp"].ToString(), "o", System.Globalization.CultureInfo.InvariantCulture)
                            });
                        }
                    }
                }
                Debug.WriteLine($"Found {deviceDataList.Count} device data records.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GetDeviceData (all) failed: {ex.Message}\nStackTrace: {ex.StackTrace}");
                throw;
            }
            return deviceDataList;
        }

        public List<DeviceData> GetDeviceData(string deviceName)
        {
            var deviceDataList = new List<DeviceData>();
            try
            {
                Debug.WriteLine($"Querying device data for device: {deviceName}");
                using (var connection = new SqliteConnection($"Data Source={_dbPath}"))
                {
                    connection.Open();
                    var command = connection.CreateCommand();
                    command.CommandText = "SELECT * FROM DeviceData WHERE Name = @deviceName";
                    command.Parameters.AddWithValue("@deviceName", deviceName);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            deviceDataList.Add(new DeviceData
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                Name = reader["Name"].ToString(),
                                IpAddress = reader["IpAddress"].ToString(),
                                SlaveId = Convert.ToInt32(reader["SlaveId"]),
                                IsOperational = Convert.ToBoolean(reader["IsOperational"]),
                                RunCount = Convert.ToInt32(reader["RunCount"]),
                                Timestamp = reader.IsDBNull(reader.GetOrdinal("Timestamp")) ? DateTime.MinValue : DateTime.Parse(reader["Timestamp"].ToString())
                            });
                        }
                    }
                }
                Debug.WriteLine($"Found {deviceDataList.Count} records for device: {deviceName}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GetDeviceData failed: {ex.Message}\nStackTrace: {ex.StackTrace}");
                throw;
            }
            return deviceDataList;
        }

        public void SaveDeviceData(int deviceIndex, string name, string ipAddress, int slaveId, bool isOperational, int runCount)
        {
            try
            {
                Debug.WriteLine($"Saving device data for device: {name}");
                using (var connection = new SqliteConnection($"Data Source={_dbPath}"))
                {
                    connection.Open();
                    var command = connection.CreateCommand();
                    command.CommandText = @"
                        INSERT OR REPLACE INTO DeviceData (Id, Name, IpAddress, SlaveId, IsOperational, RunCount, Timestamp)
                        VALUES (@id, @name, @ipAddress, @slaveId, @isOperational, @runCount, @timestamp)";
                    command.Parameters.AddWithValue("@id", deviceIndex + 1);
                    command.Parameters.AddWithValue("@name", name);
                    command.Parameters.AddWithValue("@ipAddress", ipAddress);
                    command.Parameters.AddWithValue("@slaveId", slaveId);
                    command.Parameters.AddWithValue("@isOperational", isOperational ? 1 : 0);
                    command.Parameters.AddWithValue("@runCount", runCount);
                    command.Parameters.AddWithValue("@timestamp", DateTime.Now.ToString("o"));
                    command.ExecuteNonQuery();
                    Debug.WriteLine("Device data saved successfully.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SaveDeviceData failed: {ex.Message}\nStackTrace: {ex.StackTrace}");
                throw;
            }
        }


        public bool DeviceExists(int deviceId)
        {
            try
            {
                using (var connection = new SqliteConnection($"Data Source={_dbPath}"))
                {
                    connection.Open();
                    var command = connection.CreateCommand();
                    command.CommandText = "SELECT COUNT(*) FROM DeviceData WHERE Id = @id";
                    command.Parameters.AddWithValue("@id", deviceId);
                    var count = (long)command.ExecuteScalar();
                    Debug.WriteLine($"DeviceExists: DeviceId={deviceId}, Exists={count > 0}");
                    return count > 0;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"DeviceExists failed: {ex.Message}\nStackTrace: {ex.StackTrace}");
                return false;
            }
        }

        // 新增设备记录
        public void AddDeviceRecord(DeviceRecord record)
        {
            try
            {
                Debug.WriteLine($"Adding device record for device: {record.DeviceName}, DeviceId={record.DeviceId}");
                if (!DeviceExists(record.DeviceId))
                {
                    throw new InvalidOperationException($"設備 ID {record.DeviceId} 不存在於 DeviceData 表中。");
                }

                using (var connection = new SqliteConnection($"Data Source={_dbPath}"))
                {
                    connection.Open();
                    using (var transaction = connection.BeginTransaction())
                    {
                        var command = connection.CreateCommand();
                        command.CommandText = $@"
                            INSERT INTO DeviceRecords_{record.DeviceId} (DeviceName, Username, RunCount, Content, Timestamp)
                            VALUES (@deviceName, @username, @runcount, @content, @timestamp)";
                        command.Parameters.AddWithValue("@deviceName", record.DeviceName);
                        command.Parameters.AddWithValue("@username", record.Username);
                        command.Parameters.AddWithValue("@runcount", record.RunCount);
                        command.Parameters.AddWithValue("@content", record.Content);
                        command.Parameters.AddWithValue("@timestamp", record.Timestamp.ToString("o"));
                        command.ExecuteNonQuery();

                        transaction.Commit();
                        Debug.WriteLine("Device record added successfully.");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"AddDeviceRecord failed: {ex.Message}\nStackTrace: {ex.StackTrace}");
                throw;
            }
        }

        // 获取设备记录
        public List<DeviceRecord> GetDeviceRecords(int deviceId)
        {
            var records = new List<DeviceRecord>();
            try
            {
                Debug.WriteLine($"Querying records for device ID: {deviceId}");
                using (var connection = new SqliteConnection($"Data Source={_dbPath}"))
                {
                    connection.Open();
                    var command = connection.CreateCommand();
                    command.CommandText = $"SELECT * FROM DeviceRecords_{deviceId} ORDER BY Timestamp DESC";
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            records.Add(new DeviceRecord
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                DeviceId = deviceId, // 手動設置 DeviceId
                                DeviceName = reader["DeviceName"].ToString(),
                                Username = reader["Username"].ToString(),
                                RunCount = Convert.ToInt32(reader["RunCount"]),
                                Content = reader["Content"].ToString(),
                                Timestamp = DateTime.ParseExact(reader["Timestamp"].ToString(), "o", System.Globalization.CultureInfo.InvariantCulture)
                            });
                        }
                    }
                }
                Debug.WriteLine($"Found {records.Count} records for device ID: {deviceId}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GetDeviceRecords failed: {ex.Message}\nStackTrace: {ex.StackTrace}");
                throw;
            }
            return records;
        }

        // 获取所有设备记录
        public List<DeviceRecord> GetAllDeviceRecords()
        {
            var records = new List<DeviceRecord>();
            try
            {
                Debug.WriteLine("Querying all device records.");
                using (var connection = new SqliteConnection($"Data Source={_dbPath}"))
                {
                    connection.Open();
                    for (int deviceId = 1; deviceId <= 12; deviceId++)
                    {
                        var command = connection.CreateCommand();
                        command.CommandText = $"SELECT * FROM DeviceRecords_{deviceId} ORDER BY Timestamp DESC";
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                records.Add(new DeviceRecord
                                {
                                    Id = Convert.ToInt32(reader["Id"]),
                                    DeviceId = deviceId,
                                    DeviceName = reader["DeviceName"].ToString(),
                                    Username = reader["Username"].ToString(),
                                    RunCount = Convert.ToInt32(reader["RunCount"]),
                                    Content = reader["Content"].ToString(),
                                    Timestamp = DateTime.ParseExact(reader["Timestamp"].ToString(), "o", System.Globalization.CultureInfo.InvariantCulture)
                                });
                            }
                        }
                    }
                }
                Debug.WriteLine($"Found {records.Count} device records in total.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GetAllDeviceRecords failed: {ex.Message}\nStackTrace: {ex.StackTrace}");
                throw;
            }
            return records;
        }


        public bool DeleteDeviceRecord(int deviceId, int recordId)
        {
            try
            {
                Debug.WriteLine($"Deleting device record with ID: {recordId} for device: {deviceId}");
                using (var connection = new SqliteConnection($"Data Source={_dbPath}"))
                {
                    connection.Open();
                    var command = connection.CreateCommand();
                    command.CommandText = $"DELETE FROM DeviceRecords_{deviceId} WHERE Id = @id";
                    command.Parameters.AddWithValue("@id", recordId);
                    int rowsAffected = command.ExecuteNonQuery();
                    Debug.WriteLine($"DeleteDeviceRecord: {rowsAffected} rows affected");
                    return rowsAffected > 0;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"DeleteDeviceRecord failed: {ex.Message}\nStackTrace: {ex.StackTrace}");
                throw;
            }
        }

    }
}
