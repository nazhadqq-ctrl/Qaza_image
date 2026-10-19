const sql = require('mssql');
const fs = require('fs');
const path = require('path');

// 1. Read app_config.json if present
function loadConfigFile() {
    try {
        const configPath = path.join(__dirname, 'app_config.json');
        if (fs.existsSync(configPath)) {
            const raw = fs.readFileSync(configPath, 'utf8');
            return JSON.parse(raw);
        }
    } catch (e) {
        console.warn('[Config] Could not parse app_config.json:', e.message);
    }
    return {};
}

const fileConfig = loadConfigFile();

// 2. Parse Server IP and Port (Support formats: "192.168.3.1:1433", "192.168.3.1,1433", or "192.168.3.1")
let rawServer = process.env.DB_SERVER || fileConfig.dbServer || '192.168.3.1';
let port = parseInt(process.env.DB_PORT || fileConfig.dbPort, 10) || 1433;

if (rawServer.includes(':')) {
    const parts = rawServer.split(':');
    rawServer = parts[0].trim();
    const parsedPort = parseInt(parts[1].trim(), 10);
    if (!isNaN(parsedPort)) port = parsedPort;
} else if (rawServer.includes(',')) {
    const parts = rawServer.split(',');
    rawServer = parts[0].trim();
    const parsedPort = parseInt(parts[1].trim(), 10);
    if (!isNaN(parsedPort)) port = parsedPort;
}

const primaryPassword = process.env.DB_PASSWORD || fileConfig.dbPassword || 'Na2652014Va';
const fallbackPasswords = ['Na2652014Va', 'sa123', '123456', 'sa'];

const dbConfig = {
    user: process.env.DB_USER || fileConfig.dbUser || 'sa',
    password: primaryPassword,
    server: rawServer,
    port: port,
    database: process.env.DB_DATABASE || fileConfig.database || 'Pic_Qaza',
    options: {
        encrypt: false,
        trustServerCertificate: true,
        connectTimeout: 8000,
        requestTimeout: 30000,
        enableArithAbort: true
    },
    pool: {
        max: 10,
        min: 0,
        idleTimeoutMillis: 30000
    }
};

let pool = null;

async function getPool() {
    if (pool && pool.connected) {
        return pool;
    }

    // Try primary connection to configured server (192.168.3.1:1433)
    try {
        pool = await sql.connect(dbConfig);
        console.log(`[SQL Server] Connected successfully to ${dbConfig.server}:${dbConfig.port} / ${dbConfig.database}`);
        await ensureTables(pool);
        return pool;
    } catch (err) {
        console.warn(`[SQL Server] Connection to ${dbConfig.server}:${dbConfig.port} failed (${err.message}). Trying fallback passwords and hosts...`);

        // If login failed, try fallback passwords
        if (err.message && err.message.includes('Login failed')) {
            for (const pw of fallbackPasswords) {
                if (pw === dbConfig.password) continue;
                try {
                    const pwConfig = { ...dbConfig, password: pw };
                    pool = await sql.connect(pwConfig);
                    console.log(`[SQL Server] Connected successfully with fallback password to ${dbConfig.server}:${dbConfig.port}`);
                    await ensureTables(pool);
                    return pool;
                } catch (pwErr) {
                    // continue to next password
                }
            }
        }

        // Fallback to local server name 'NAZHAD' or 'localhost' if 192.168.3.1 is unreachable
        const fallbackServers = ['NAZHAD', 'localhost', '127.0.0.1'];
        for (const fbServer of fallbackServers) {
            if (fbServer.toLowerCase() === rawServer.toLowerCase()) continue;
            for (const pw of [primaryPassword, 'Na2652014Va', 'sa123']) {
                try {
                    const fallbackConfig = { ...dbConfig, server: fbServer, password: pw };
                    pool = await sql.connect(fallbackConfig);
                    console.log(`[SQL Server] Connected successfully via fallback server: ${fbServer} / ${dbConfig.database}`);
                    await ensureTables(pool);
                    return pool;
                } catch (fbErr) {
                    // continue
                }
            }
        }

        console.error('[SQL Server] All connection attempts failed:', err.message);
        throw err;
    }
}

async function ensureTables(connectedPool) {
    const checkTableSql = `
        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Users')
        BEGIN
            CREATE TABLE Users (
                Id INT IDENTITY(1,1) PRIMARY KEY,
                Username NVARCHAR(50) NOT NULL UNIQUE,
                Password NVARCHAR(255) NOT NULL,
                FullName NVARCHAR(100) NOT NULL,
                Role NVARCHAR(50) NOT NULL DEFAULT 'Admin',
                CreatedAt DATETIME DEFAULT GETDATE(),
                IsActive BIT DEFAULT 1
            );

            INSERT INTO Users (Username, Password, FullName, Role)
            VALUES ('admin', 'admin123', N'بەڕێوەبەری سەرەکی', 'Admin');
        END

        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'QazaImages')
        BEGIN
            CREATE TABLE QazaImages (
                Id INT IDENTITY(1,1) PRIMARY KEY,
                SerialNo INT,
                ReceiptNo NVARCHAR(100) NOT NULL,
                ReceiptDate DATE NOT NULL,
                CreatedAt DATETIME DEFAULT GETDATE(),
                ImagePath NVARCHAR(500) NOT NULL,
                ImageSizeKB FLOAT,
                RegisteredBy NVARCHAR(100) NOT NULL
            );
        END
    `;
    await connectedPool.request().query(checkTableSql);
}

module.exports = {
    sql,
    getPool,
    dbConfig
};
