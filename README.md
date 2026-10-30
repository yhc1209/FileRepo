# 📦 檔案儲存庫系統 (FileRepo)

基於 **ASP.NET Core 9.0 Web API** 與 **Angular 19** 構建的檔案儲存庫網站，支援 Docker 一鍵啟動與 HTTP Range 斷點續傳下載。

---

## 🚀 主要功能

1. **檔案上傳 GUI**：使用者可透過 Web 介面選擇/拖曳檔案上傳，並即時顯示上傳進度條。
2. **瀏覽已上傳檔案 GUI**：展示 Server 上所有檔案的元資料（包含檔名、格式化檔案大小、SHA-256 雜湊值與上傳日期）。
3. **檔案下載 GUI**：點擊按鈕即可下載指定的檔案。

---

## 🔌 擴充功能 (對外下載器整合 API)

### 1. 枚舉檔案 (GET)
- **Endpoint**: `GET /api/files`
- **說明**: 查詢 Server 當前所有檔案的相關資訊列表。
- **cURL 範例**:
  ```bash
  curl -X GET http://localhost:8080/api/files
  ```

### 2. 下載資訊 (POST)
- **Endpoint**: `POST /api/files/download-info`
- **Request Body**:
  ```json
  {
    "fileId": "YOUR-FILE-GUID-HERE"
  }
  ```
- **cURL 範例**:
  ```bash
  curl -X POST http://localhost:8080/api/files/download-info \
    -H "Content-Type: application/json" \
    -d '{"fileId":"3fa85f64-5717-4562-b3fc-2c963f66afa6"}'
  ```

### 3. 下載檔案 / 斷點續傳 (GET & HEAD)
- **Endpoint**: `GET /api/files/{id}/download` 或 `HEAD /api/files/{id}/download`
- **支援功能**:
  - **HEAD 方法**: 僅獲取 Header 資訊（如 `Content-Length`、`Accept-Ranges`）。
  - **HTTP Range 斷點續傳**: 帶上 `Range: bytes=start-end` 標頭，Server 會回傳 `206 Partial Content` 區段資料。
- **cURL 斷點續傳測試範例**:
  ```bash
  # 測試 HEAD 請求
  curl -i -I http://localhost:8080/api/files/3fa85f64-5717-4562-b3fc-2c963f66afa6/download

  # 測試 Range 續傳（下載前 100 Bytes）
  curl -i -H "Range: bytes=0-99" http://localhost:8080/api/files/3fa85f64-5717-4562-b3fc-2c963f66afa6/download
  ```

---

## 🛠️ 建置 Docker Image 與執行說明

### 前置需求
- 已安裝 Docker 與 Docker Compose。

### 1. 一鍵構建與啟動服務
於專案根目錄下執行：
```bash
docker-compose up --build -d
```

### 2. 開啟網站
- **前端 GUI 介面**: 開啟瀏覽器存取 `http://localhost:8080`
- **後端 Swagger API 文件**: 開啟 `http://localhost:5000/swagger`

### 3. 停止服務
```bash
docker-compose down
```
