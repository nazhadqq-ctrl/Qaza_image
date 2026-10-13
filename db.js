const sql = require('mssql');

const dbConfig = {
    user: process.env.DB_USER || 'sa',
    password: process.env.DB_PASSWORD || 'Na2652014Va',
    server: process.env.DB_SERVER || 'localhost', // 'NAZHAD' or 'localhost'
    database: process.env.DB_DATABASE || 'Pic_Qaza',
    options: {
        encrypt: false,
        trustServerCertificate: true,
        connectTimeout: 10000,
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

    try {
        // Try connecting with 'localhost' or fallback to 'NAZHAD'
        pool = await sql.connect(dbConfig);
        console.log(`[SQL Server] Connected successfully to ${dbConfig.server} / ${dbConfig.database}`);
        await ensureTables(pool);
        return pool;
    } catch (err) {
        console.warn(`[SQL Server] Connection to ${dbConfig.server} failed, attempting fallback to NAZHAD...`);
        try {
            const fallbackConfig = { ...dbConfig, server: 'NAZHAD' };
            pool = await sql.connect(fallbackConfig);
            console.log(`[SQL Server] Connected successfully via fallback to NAZHAD / ${dbConfig.database}`);
            await ensureTables(pool);
            return pool;
        } catch (fallbackErr) {
            console.error('[SQL Server] Connection Error:', fallbackErr.message);
            throw fallbackErr;
        }
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
