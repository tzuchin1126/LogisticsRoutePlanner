# 物流配送路線優化系統
<img width="1468" alt="iShot_2025-05-05_19 35 34" src="https://github.com/user-attachments/assets/42d8da9a-26e5-4dd2-8caf-51e352874c0c" />
(頁面上的地址均為google地圖上隨意找尋做範例)


這是一個基於 **ASP.NET Core MVC** 開發的物流配送路線優化系統，旨在幫助企業根據出貨地點與客戶地點自動生成最優配送路線。系統提供了匯入功能，能夠批量處理配送清單，並根據實際需求優化配送順序。

## 功能特點

- **靈活的配送路線生成**：系統能根據使用者輸入的出貨地點及多個客戶地點，智能排列最順暢的配送路線，提升物流效率。
- **匯入與輸出功能**：支持匯入 Excel 格式的配送清單，並能匯出整理後的報表。
- **配送狀態管理**：使用者可以根據配送狀況標註每一筆配送紀錄，如「已送達」、「待送達」、「跳過」等。
- **Google API 路徑優化**：利用 Google API 進行路徑優化，根據實際地址計算最短路線。

## 技術堆疊

- **前端**：
  - **HTML / CSS / JavaScript**：使用 Bootstrap 和自訂 CSS 提供響應式界面，適用於各種設備。
  - **Font Awesome**：用於顯示圖標，提升使用者體驗。

- **後端**：
  - **ASP.NET Core MVC**：使用 MVC 架構進行開發，便於維護與擴展。
  - **MySQL**：作為資料庫，存儲配送紀錄、路線等相關資訊。

- **其他工具**：
  - **Google Maps API**：用於路徑規劃與地址解析。

## 安裝與使用

1. **克隆專案**
   ```bash
   git clone https://github.com/tzuchin1126/LogisticsRoutePlanner.git
