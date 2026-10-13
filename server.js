const express = require('express');
const cors = require('cors');
const path = require('path');
const { getPool, sql, dbConfig } = require('./db');

const fs = require('fs');

const app = express();
const PORT = process.env.PORT || 4500;

app.use(cors());
app.use(express.json({ limit: '50mb' }));
app.use(express.urlencoded({ limit: '50mb', extended: true }));

// Serve static frontend files
app.use(express.static(path.join(__dirname, 'public')));

// Database connection health & status
app.get('/api/status', async (req, res) => {
    try {
        const pool = await getPool();
        const result = await pool.request().query(`
            SELECT 
                @@SERVERNAME AS ServerName,
                DB_NAME() AS DatabaseName,
                (SELECT COUNT(*) FROM Users) AS TotalUsers,
                (SELECT COUNT(*) FROM QazaImages) AS TotalImages
        `);
        res.json({
            connected: true,
            server: result.recordset[0].ServerName,
            database: result.recordset[0].DatabaseName,
            totalUsers: result.recordset[0].TotalUsers,
            totalImages: result.recordset[0].TotalImages,
            configServer: dbConfig.server,
            timestamp: new Date().toISOString()
        });
    } catch (err) {
        res.status(500).json({
            connected: false,
            error: err.message,
            server: dbConfig.server,
            database: dbConfig.database
        });
    }
});

// Authentication endpoint
app.post('/api/login', async (req, res) => {
    const { username, password } = req.body;

    if (!username || !password) {
        return res.status(400).json({
            success: false,
            message: 'تکایە ناوی بەکارهێنەر و وشەی نهێنی بنووسە.'
        });
    }

    try {
        const pool = await getPool();
        const request = pool.request();
        request.input('username', sql.NVarChar(50), username.trim());
        request.input('password', sql.NVarChar(255), password.trim());

        const result = await request.query(`
            SELECT Id, Username, FullName, Role, CreatedAt, IsActive 
            FROM Users 
            WHERE Username = @username AND Password = @password AND IsActive = 1
        `);

        if (result.recordset.length === 0) {
            return res.status(401).json({
                success: false,
                message: 'ناوی بەکارهێنەر یان وشەی نهێنی هەڵەیە!'
            });
        }

        const user = result.recordset[0];
        res.json({
            success: true,
            message: 'چوونەژوورەوە بە سەرکەوتوویی ئەنجامدرا',
            user: {
                id: user.Id,
                username: user.Username,
                fullName: user.FullName,
                role: user.Role,
                createdAt: user.CreatedAt
            }
        });
    } catch (err) {
        console.error('[Login Error]', err);
        res.status(500).json({
            success: false,
            message: 'هەڵە لە پەیوەندی بە بنکەی دراوە: ' + err.message
        });
    }
});

// Get users list
app.get('/api/users', async (req, res) => {
    try {
        const pool = await getPool();
        const result = await pool.request().query(`
            SELECT Id, Username, FullName, Role, CreatedAt, IsActive 
            FROM Users 
            ORDER BY Id ASC
        `);
        res.json({ success: true, users: result.recordset });
    } catch (err) {
        res.status(500).json({ success: false, message: err.message });
    }
});

// Add new user
app.post('/api/users', async (req, res) => {
    const { username, password, fullName, role } = req.body;

    if (!username || !password || !fullName) {
        return res.status(400).json({
            success: false,
            message: 'تکایە هەموو خانە پێویستەکان پڕبکەرەوە.'
        });
    }

    try {
        const pool = await getPool();
        const request = pool.request();
        request.input('username', sql.NVarChar(50), username.trim());
        request.input('password', sql.NVarChar(255), password.trim());
        request.input('fullName', sql.NVarChar(100), fullName.trim());
        request.input('role', sql.NVarChar(50), role ? role.trim() : 'User');

        // Check if username already exists
        const check = await pool.request()
            .input('u', sql.NVarChar(50), username.trim())
            .query('SELECT Id FROM Users WHERE Username = @u');

        if (check.recordset.length > 0) {
            return res.status(400).json({
                success: false,
                message: 'ئەم ناوی بەکارهێنەرە پێشتر بەکارهاتووە.'
            });
        }

        await request.query(`
            INSERT INTO Users (Username, Password, FullName, Role, CreatedAt, IsActive)
            VALUES (@username, @password, @fullName, @role, GETDATE(), 1)
        `);

        res.json({ success: true, message: 'بەکارهێنەری نوێ بە سەرکەوتوویی زیادکرا' });
    } catch (err) {
        res.status(500).json({ success: false, message: err.message });
    }
});

// Change user password
app.put('/api/users/:id/password', async (req, res) => {
    const { id } = req.params;
    const { newPassword } = req.body;

    if (!newPassword) {
        return res.status(400).json({ success: false, message: 'وشەی نهێنی نوێ بنووسە.' });
    }

    try {
        const pool = await getPool();
        const request = pool.request();
        request.input('id', sql.Int, parseInt(id));
        request.input('newPassword', sql.NVarChar(255), newPassword.trim());

        await request.query(`
            UPDATE Users 
            SET Password = @newPassword 
            WHERE Id = @id
        `);

        res.json({ success: true, message: 'وشەی نهێنی بە سەرکەوتوویی نوێکرایەوە' });
    } catch (err) {
        res.status(500).json({ success: false, message: err.message });
    }
});

// Delete user
app.delete('/api/users/:id', async (req, res) => {
    const { id } = req.params;

    try {
        const pool = await getPool();
        const request = pool.request();
        request.input('id', sql.Int, parseInt(id));

        // Prevent deleting the main admin with id 1
        if (parseInt(id) === 1) {
            return res.status(400).json({
                success: false,
                message: 'سڕینەوەی بەڕێوەبەری سەرەکی رێگەپێنەدراوە.'
            });
        }

        await request.query(`DELETE FROM Users WHERE Id = @id`);
        res.json({ success: true, message: 'بەکارهێنەر سڕایەوە' });
    } catch (err) {
        res.status(500).json({ success: false, message: err.message });
    }
});

// ================= QAZA IMAGES API =================

// Get next serial number
app.get('/api/images/next-serial', async (req, res) => {
    try {
        const pool = await getPool();
        const result = await pool.request().query('SELECT ISNULL(MAX(SerialNo), 0) + 1 AS NextSerial FROM QazaImages');
        res.json({ success: true, nextSerial: result.recordset[0].NextSerial });
    } catch (err) {
        res.status(500).json({ success: false, message: err.message });
    }
});

// List registered images: Today only by default, or all records when searching
app.get('/api/images', async (req, res) => {
    try {
        const { search } = req.query;
        const pool = await getPool();
        const request = pool.request();
        let sqlQuery = 'SELECT Id, SerialNo, ReceiptNo, CONVERT(VARCHAR(10), ReceiptDate, 120) AS ReceiptDate, CONVERT(VARCHAR(8), CreatedAt, 108) AS CreatedTime, CONVERT(VARCHAR(19), CreatedAt, 120) AS CreatedAtLocal, CreatedAt, ImagePath, ImageSizeKB, RegisteredBy FROM QazaImages';
        
        let isTodayOnly = false;
        if (search && search.trim()) {
            // Search across all historical records
            request.input('q', sql.NVarChar(100), `%${search.trim()}%`);
            sqlQuery += ' WHERE ReceiptNo LIKE @q OR CAST(SerialNo AS NVARCHAR) LIKE @q OR RegisteredBy LIKE @q';
        } else {
            // Default: Show only today's records to prevent server load
            isTodayOnly = true;
            sqlQuery += ' WHERE (ReceiptDate = CAST(GETDATE() AS DATE) OR CAST(CreatedAt AS DATE) = CAST(GETDATE() AS DATE))';
        }
        
        sqlQuery += ' ORDER BY SerialNo DESC';
        const result = await request.query(sqlQuery);
        res.json({ success: true, images: result.recordset, isTodayOnly });
    } catch (err) {
        res.status(500).json({ success: false, message: err.message });
    }
});

// Register new receipt image
app.post('/api/images', async (req, res) => {
    const { serialNo, receiptNo, receiptDate, imageBase64, registeredBy } = req.body;

    if (!receiptNo || !receiptDate || !imageBase64) {
        return res.status(400).json({
            success: false,
            message: 'تکایە هەموو خانە پێویستەکان (ژمارەی پسووڵە، بەروار، وێنە) پڕبکەرەوە.'
        });
    }

    try {
        // Decode base64 image and save
        const matches = imageBase64.match(/^data:image\/([a-zA-Z0-9+]+);base64,(.+)$/);
        let ext = 'jpg';
        let buffer;
        if (matches) {
            ext = matches[1] === 'jpeg' ? 'jpg' : matches[1];
            buffer = Buffer.from(matches[2], 'base64');
        } else {
            buffer = Buffer.from(imageBase64, 'base64');
        }

        const filename = `receipt_${Date.now()}_${Math.floor(Math.random() * 1000)}.${ext}`;
        const uploadsDir = path.join(__dirname, 'public', 'uploads');
        if (!fs.existsSync(uploadsDir)) {
            fs.mkdirSync(uploadsDir, { recursive: true });
        }
        const filePath = path.join(uploadsDir, filename);
        fs.writeFileSync(filePath, buffer);

        const sizeKB = parseFloat((buffer.length / 1024).toFixed(2));
        const webPath = `/uploads/${filename}`;

        const pool = await getPool();
        
        let finalSerial = parseInt(serialNo);
        if (!finalSerial || isNaN(finalSerial)) {
            const serialRes = await pool.request().query('SELECT ISNULL(MAX(SerialNo), 0) + 1 AS NextSerial FROM QazaImages');
            finalSerial = serialRes.recordset[0].NextSerial;
        }

        const request = pool.request();
        request.input('serialNo', sql.Int, finalSerial);
        request.input('receiptNo', sql.NVarChar(100), receiptNo.trim());
        request.input('receiptDate', sql.Date, receiptDate);
        request.input('imagePath', sql.NVarChar(500), webPath);
        request.input('imageSizeKB', sql.Float, sizeKB);
        request.input('registeredBy', sql.NVarChar(100), registeredBy ? registeredBy.trim() : 'نەزانراو');

        await request.query(`
            INSERT INTO QazaImages (SerialNo, ReceiptNo, ReceiptDate, CreatedAt, ImagePath, ImageSizeKB, RegisteredBy)
            VALUES (@serialNo, @receiptNo, @receiptDate, GETDATE(), @imagePath, @imageSizeKB, @registeredBy)
        `);

        res.json({
            success: true,
            message: 'وێنە و پسووڵە بە سەرکەوتوویی تۆمارکرا',
            record: {
                serialNo: finalSerial,
                receiptNo: receiptNo.trim(),
                receiptDate,
                imagePath: webPath,
                imageSizeKB: sizeKB,
                registeredBy: registeredBy || 'نەزانراو'
            }
        });
    } catch (err) {
        console.error('[Save Image Error]', err);
        res.status(500).json({ success: false, message: 'هەڵە لە پاشەکەوتکردن: ' + err.message });
    }
});

// Delete receipt image
app.delete('/api/images/:id', async (req, res) => {
    const { id } = req.params;

    try {
        const pool = await getPool();
        const request = pool.request();
        request.input('id', sql.Int, parseInt(id));

        const selectRes = await request.query('SELECT ImagePath FROM QazaImages WHERE Id = @id');
        if (selectRes.recordset.length > 0) {
            const imgRelPath = selectRes.recordset[0].ImagePath;
            const fullPath = path.join(__dirname, 'public', imgRelPath.replace(/^\//, ''));
            if (fs.existsSync(fullPath)) {
                try { fs.unlinkSync(fullPath); } catch (e) {}
            }
        }

        await pool.request().input('id', sql.Int, parseInt(id)).query('DELETE FROM QazaImages WHERE Id = @id');
        res.json({ success: true, message: 'وێنە و تۆمارەکە بە سەرکەوتوویی سڕایەوە' });
    } catch (err) {
        res.status(500).json({ success: false, message: err.message });
    }
});

// Fallback to index.html for SPA
app.use((req, res) => {
    res.sendFile(path.join(__dirname, 'public', 'index.html'));
});

app.listen(PORT, '0.0.0.0', async () => {
    console.log(`====================================================`);
    console.log(`  Pic_Qaza Desktop Application Server`);
    console.log(`  Running at: http://localhost:${PORT}`);
    console.log(`  Running at: http://127.0.0.1:${PORT}`);
    console.log(`====================================================`);
    try {
        await getPool();
    } catch (e) {
        console.error('[Startup Warning] SQL connection will retry on request.');
    }
});
