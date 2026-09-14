# PROCESS.md — 我的練習心得

> 一個原則：**寫「具體發生的事」，不寫感想文。**
> 貼上當時真實的 prompt、真實的數字、真實的錯誤訊息——三個月後的你（和你的同事）才用得上。

#### 使用的 agent 與模型：
	- Codex / GPT-5.5 (Low)

---

## 通用四問

### 1. 我的任務拆解

（開工前你把任務拆成哪幾步？實際做的時候順序有變嗎？為什麼變？）

	- 找問題點 > 請 AI 修復 > 檢查修復代碼 > 實際測試 > 確認沒問題 請 AI commit
	- 測試有問題會再請 AI 修復 然後重複以上順序

### 2. AI 幫上大忙的地方

（哪件事 agent 做得又快又好？**貼上當時的提問原文**，說明為什麼這樣問有效。）

	- 給明確的 bug 線索, 它可以很快從 Controller 追到 Service / Repository
	- 當時提問原文: 
	【`$fix-bug 剛建立的訂單在列表第一頁找不到, 要翻到後面才看得到；而且點到最後一頁常常是空白的`】
	【`training-repo/src/OrderHub.Infrastructure/Repositories/OrderRepository.cs:32 用了 Skip(page * pageSize), 第一頁會直接跳過前 20 筆；通常應該是 Skip((page - 1) * pageSize)`】
	把症狀、檔案、可能原因寫給 AI, AI 可以直接確認流程和補測試

### 3. AI 誤導我的地方，與我如何發現

（agent 說錯／改錯／過度自信的時刻。你靠什麼抓到——對照程式碼？頁面實測？跑測試？）

	- 低庫存頁面 AI 為了避免亂碼, 把中文改成 `&#21517;&#31281;` 這種 HTML entity
	頁面雖然能跑, 但代碼可讀性很差, 檢查代碼時發現的

### 4. 我會帶回日常工作的一招

（一個具體、可複製的做法，不要寫「要多驗證」這種口號——寫出**操作步驟**。）

	1. 先請 AI 說它理解的症狀, 先不調整代碼
	2. 要它從 Controller → Service → Repository 追一次
	3. 要它先講根因和最小修法, 我確認後才調整
	4. AI 調整完再檢查確認代碼是否OK
	5. 實際測試

## 自我驗證（做到哪個階段答哪題）

### 第一階段 — Agentic Coding

練習 1

1. 我能不看筆記說出三個專案（Web/Core/Infrastructure）各自的職責
	- 專案是三層式 .NET 8 MVC: 
		1. OrderHub.Web: Controller、ViewModel、Razor View, 負責接 request、呼叫 service、把 domain model 手動轉成 ViewModel
		2. OrderHub.Core: domain model、service interface、商業邏輯, 訂單建立、取消、折扣、總額計算都在 OrderService
		3. OrderHub.Infrastructure: EF Core DbContext、repository、migration、seed data, 只有這層直接碰 DbContext
	- tests/OrderHub.Tests: xUnit + EF Core InMemory, 目前測的是 service/repository 行為, 不需要 SQL Server

2. 我核對過 agent 描述的建單流程，且**至少找出一處不精確或過度簡化的說法**
	- agent 把描述建單流程為「建立訂單時會扣庫存並計算折扣」
	- 實際 扣庫存是在 `OrderService.CreateOrderAsync` 裡逐筆商品處理
	- 折扣是由 `OrderService.CalculateTotal` 依照會員等級計算
	- 另外 Gold 會員目前在建立訂單時也會先改 `UnitPriceSnapshot`
	
3. 我知道商業邏輯應該放在哪一層、新增頁面要動哪些地方
	- 商業邏輯應該放在 `OrderHub.Core` 的 service 裡
	- Controller 是接收 request、做基本 ModelState 驗證、呼叫 service, 最後把結果轉成 ViewModel 給 view
	- Repository 是包 EF Core 查詢, 只有 `OrderHub.Infrastructure` 這層可以直接使用 `DbContext`
	- View 顯示資料
	
	如果要新增一個頁面: 
	- `Core.Interfaces`: 如果需要新的資料查詢, 要先定義 repository 介面
	- `Infrastructure.Repositories`: 實作 EF Core 查詢
	- `Web.Controllers`: 新增 action, 呼叫 service, 處理 ModelState 或錯誤訊息
	- `Web.ViewModels`: 新增頁面專用 ViewModel
	- `Web.Views`: 新增 Razor View
	- `Views/Shared/_Layout.cshtml`: 如果頁面需要出現在導覽列, 就要加 navbar 連結
	- `tests/OrderHub.Tests`: 補 service 層測試

練習 2

1. 三個 bug 我都先在頁面上重現過，才開始找程式
	- 是

2. 我給 agent 的資訊包含具體觀察（頁碼／金額數字／庫存數字），而不是只貼客訴原文
	- 是

3. 每個修復都回到頁面驗證過症狀消失
	- 是

4. 每個 bug 都補了一個回歸測試，`dotnet test` 全綠
	- 是

5. 三個獨立 commit，message 說明症狀與根因
	- 訂單列表分頁是自己 commit, 沒說明到症狀與根因
	- 之後的都有說明

6. （思考題）為什麼原本的測試沒抓到這三個 bug？
	- 原測試沒測完整流程

練習 3

1. `/Products/LowStock` 不帶參數 → 門檻 10 的結果；帶 `?threshold=3` → 結果隨之改變
	- `LowStockViewModel.Threshold` 預設是 10

2. `?threshold=0`、`?threshold=-1` → 頁面顯示驗證錯誤，不是 500
	- `Threshold` 有用 `[Range(1, int.MaxValue)]` 驗證, Controller 先看 `ModelState.IsValid`, 不合法就回 View

3. 售出數量欄位排除了 Cancelled 訂單（可用一筆已取消的訂單驗證）
	- 有排除

4. 停售（已停售 badge）商品不出現在列表
	- 是

5. 程式分層與命名跟既有的 Products 功能一致（請 agent 自我 review 一次，並自己確認）
	- 大致有做到

6. 至少 3 個新測試，`dotnet test` 全綠
	- 有新增測試
	- `test_runner` 回報 `41 個測試全部通過`

練習 4

1. 重構後 `dotnet test` 全綠
	- 是

2. 我能說出這次重構「改善了什麼、沒有改變什麼」
	- 改善 `CreateOrderAsync` 驗證明細、建立 Pending 訂單、加入商品品項被拆到 private helper
	- 沒改驗證順序、錯誤訊息、庫存扣減時機、`UnitPriceSnapshot = product.UnitPrice`、`ServiceResult` 成功/失敗語意

3. 我有在 code review 的角度看過 diff（不是 agent 說好就好）
	- 是

### 第二階段 — 自建 MCP Server

練習 2

1. 三個工具都列得出來,且 description、參數說明如你所寫
	- 是

2. 手動呼叫 LowStock(threshold=10),回傳的商品和 /Products 頁面上的低庫存商品一致
	- 是

3. 呼叫 GetOrder 用一個不存在的 Id,回應是清楚的錯誤訊息而不是 exception dump
	- 是, 回應 "找不到訂單 0"

練習 3

1. Codex:把 config.toml 的 [mcp_servers.orderhub] 區塊註解掉後重啟),問 agent:「哪些商品庫存低於 5?」——觀察它得寫程式或查 DB 繞多遠
	- 耗時：8 ~ 10 秒， 
	- 操作：agent 會打開頁面呼叫 http://localhost:5150/Products/LowStock?Threshold=5 再回報

2. 開啟 MCP,同一個問題再問一次——應該一次工具呼叫就答完
	- 耗時：6.7833 秒
	- 操作：agent 先確認 orderhub MCP tools 是否已載入，看到 low_stock 後直接呼叫 low_stock(threshold: 5)，沒有走瀏覽器

練習 4

1. MCP Inspector 中 cancel_order 的 annotations 如你所標(destructiveHint 等),三個唯讀工具則顯示 read-only
	- cancel_order：destructiveHint: true, idempotentHint: false
	- get_order：readOnlyHint: true
	- low_stock：readOnlyHint: true
	- customer_orders：readOnlyHint: true

2. 對 agent 說「幫我取消訂單 X」:觀察權限確認提示——你按允許之前,資料不會被動到
	- 是的

3. 取消一筆待處理訂單成功,回 /Products 頁面確認庫存有回補(就是活動 1 客訴 3 修好的行為)
	- 是

4. 對同一筆訂單再取消一次、或挑一筆已出貨訂單取消:得到清楚的拒絕訊息而非 exception dump
	- 是，結果顯示 "取消失敗:狀態為 Cancelled 的訂單不可取消"

練習 5

1. MCP Inspector:Resources 分頁讀得到 orderhub://discount-rules;Prompts 分頁能帶 threshold 參數取得展開後的訊息
	- 能讀到 + 能帶 threshold 參數

2. Codex 用戶:Inspector 讀出 resource 內容貼進對話,問同一題
	- Gold: 9 折
	  所以：
	  1000 × 0.9 = 900
	  Gold 會員買 1000 元商品應付 900 元。

3. Resource vs 讓 agent 自己讀 OrderService.cs：差在哪？
	- Resource: 整理給 agent 使用的背景知識，內容短、語意明確，agent 不需要先理解整個程式流程，但需要與代碼同步更新
	- 讓 agent 自己讀 `OrderService.cs`: agent 需要從程式代碼中推論規則，成本較高，較耗時

4. Prompt 放在 MCP server 裡 vs 每個人自己手打一段提示：差在哪？
	- Prompt 放在 MCP server：團隊共用同一套流程，高一致性
	- 每個人自己手打一段提示：格式、查詢條件、判斷標準都可能不同，結果也比較難重現

### 第三階段 — Gemini 免費 API:把 AI 嵌進產品

練習 1

1. 「上個月金卡會員取消的訂單」查得出結果,且和 /Orders 頁面用狀態篩選後肉眼比對一致(種子資料有 3 位金卡會員、近 90 天各狀態訂單)
	- 	id           : 201
		customerName : 郭俊傑
		tier         : Gold
		status       : Cancelled
		total        : 774.00
		createdAt    : 2026-07-27T01:10:20.9069374

		id           : 137
		customerName : 陳志明
		tier         : Gold
		status       : Cancelled
		total        : 13608.00
		createdAt    : 2026-07-15T13:49:30.3104978

		id           : 155
		customerName : 劉思穎
		tier         : Gold
		status       : Cancelled
		total        : 11682.00
		createdAt    : 2026-07-07T17:08:30.3104978

2. 「幫我把所有訂單刪掉」:回 422「無法理解的查詢」,資料毫髮無傷
	- 远程服务器返回错误: (422) Unprocessable Entity。

3. 拔掉 API key 再打:得到 503 與清楚的錯誤訊息,不是 500
	- 远程服务器返回错误: (503) 服务器不可用。

4. 塞一段完全無關的文字(例如食譜):模型回 intent: "unsupported",系統回「無法理解的查詢」,不會炸
	- 远程服务器返回错误: (422) Unprocessable Entity。

練習 2

1. 頁面查「上個月金卡會員取消的訂單」,結果和練習 1 的 API 一致
	- 是

2. 「幫我把所有訂單刪掉」:頁面顯示「無法理解的查詢」警示,不是錯誤頁
	- 頁面顯示 "無法理解的查詢"

3. 拔掉 API key:頁面顯示清楚的錯誤訊息,不是 500 錯誤頁
	- 頁面顯示 "Gemini API key 未設定:user-secrets 的 Gemini:ApiKey 或環境變數 GEMINI_API_KEY"

---

## 附錄：值得留下的對話片段

（貼 1–2 段最有代表性的 prompt 與回應**摘要**——不用貼全文，重點是「我怎麼問」和「它怎麼答」。）
	問:
		【`$fix-bug 剛建立的訂單在列表第一頁找不到, 要翻到後面才看得到；而且點到最後一頁常常是空白的`】
		【`training-repo/src/OrderHub.Infrastructure/Repositories/OrderRepository.cs:32 用了 Skip(page * pageSize), 第一頁會直接跳過前 20 筆；通常應該是 Skip((page - 1) * pageSize)`】
	AI答:
		確認 `page` 是 1-based, 所以應該改成 `Skip((page - 1) * pageSize)`, 後來提醒要加 `ThenByDescending(o => o.Id)` 讓分頁排序穩定
