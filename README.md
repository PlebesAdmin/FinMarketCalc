FinMarketCalc - Financial Markets Calculator

Layout - Designed to take up the least amount of space by fitting along the task bar.


[N][___] [A][________________________] | [P&L][___________] | STD [32nds] [→Dec] [×10✗] [✕]


Width = half the screen width. Height = taskbar height.
Drag to move. Double-click to close.

---

Build & Run

bash/cmd - Navgiate to the file location/folder and run "dotnet"

ex. C:\Users\PC\FinMarketCalc dotnet run

As a standalone .exe use

dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true

MODES (automatic)

STD — NOM must carry a value. 

N = Nominal or Size. For bonds this will 1000 = 1million 

PX A takes a value in normal decimal or 32nds if checked that calculated inline 

ie 99.23 - 99.13 the result will be (if NOM = 1000) 1000 x 10 x (99.23 - 99.13) representing the Profit & Loss

P&L = (NOM x 10) × PX_A


MAN — Manual is when NOM is 0 or empty or Null. NOM field stays visible but is dimmed and ignored.

If PX A is not a price difference to calculate P & L then it takes a full inline expression:

| Input            | 32nds ticked? | Result       |
|------------------|---------------|--------------|
| 2 + 2            | either        | 4            |
| 100 - 50         | either        | 50           |
| 99-25 + 98-16    | YES           | 197.90625    |
| 2-1              | NO            | 1            |
| 2-1              | YES           | 2.03125      |

Operators: `+`  `-`  `*`  `/`  `%`


Checkboxes

| Checkbox | Effect |
|----------|--------|
| 32nds    | Enables Treasury 32nds parsing. Unticked = `-` is always subtraction. |
| →Dec     | Converts PX A 32nds price to decimal in P&L (blue). Requires 32nds ticked. |
| ×10✗     | Disables hidden ×10 multiplier on NOM. |

---

NOM suffixes
`100k` = NOM  > 1000 → P&L uses 1000 (after ×10)
`5m` or 5000  = 5000 → P&L uses 50,000


