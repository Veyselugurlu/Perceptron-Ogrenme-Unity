"""
Builds the step-by-step perceptron training workbook (homework: compute the training of logical OR
row by row in Excel). Every value after the inputs is an Excel formula, so changing the start
weights or the training set recomputes the whole table.

  python tools/make_excel.py      -> docs/week08/excel/perceptron_adim_adim.xlsx
"""
from pathlib import Path

from openpyxl import Workbook
from openpyxl.comments import Comment
from openpyxl.formatting.rule import FormulaRule
from openpyxl.styles import Alignment, Border, Font, PatternFill, Side

OUT = Path("docs/week08/excel/perceptron_adim_adim.xlsx")
EPOCHS = 10
FIRST = 12  # first row of the step table

FONT = "Arial"
BLUE = Font(name=FONT, color="0000FF")
BOLD = Font(name=FONT, bold=True)
NORMAL = Font(name=FONT)
HEAD_FILL = PatternFill("solid", fgColor="DCE6F2")
INPUT_FILL = PatternFill("solid", fgColor="FFF2CC")
EPOCH_FILL = PatternFill("solid", fgColor="F2F2F2")
THIN = Side(style="thin", color="BFC5CD")
BOX = Border(left=THIN, right=THIN, top=THIN, bottom=THIN)
CENTER = Alignment(horizontal="center", vertical="center")

# sheet title, gate name, start weights (w1, w2, bias), truth table outputs for (0,0) (0,1) (1,0) (1,1)
SHEETS = [
    ("VEYA (OR)", "VEYA", (-0.8, -0.7, -0.3), (0, 1, 1, 1),
     "Başlangıç ağırlıkları ders örneğindeki değerlerdir (w1 = -0.8, w2 = -0.7, bias = -0.3)."),
    ("VE (AND)", "VE", (0.4, 0.2, -0.3), (0, 0, 0, 1),
     "Başlangıç ağırlıkları ödev örnek tablosundaki değerlerdir (w1 = 0.4, w2 = 0.2, bias = -0.3)."),
    ("XOR", "XOR", (-0.8, -0.7, -0.3), (0, 1, 1, 0),
     "XOR doğrusal olarak ayrılamaz: toplam hata hiçbir epoch'ta 0 olmaz."),
]


def build_sheet(ws, gate, start, desired, note):
    last = FIRST + 4 * EPOCHS - 1
    ws["A1"] = f"Perceptron ile mantıksal {gate} işleminin adım adım eğitimi"
    ws["A1"].font = Font(name=FONT, bold=True, size=13)
    ws["A2"] = "Sarı zeminli mavi hücreler değiştirilebilir; diğer bütün hücreler formüldür ve kendiliğinden yeniden hesaplanır."
    ws["A2"].font = Font(name=FONT, italic=True, color="555555")

    ws["A4"], ws["A5"], ws["A6"] = "W1 (başlangıç)", "W2 (başlangıç)", "BIAS (başlangıç)"
    for r, v in zip((4, 5, 6), start):
        ws.cell(r, 2, v).font = BLUE
        ws.cell(r, 2).fill = INPUT_FILL
        ws.cell(r, 2).number_format = "0.0"
        ws.cell(r, 1).font = BOLD
    ws["B4"].comment = Comment(note, "Veysel Uğurlu")

    ws["D3"] = "Eğitim kümesi"
    ws["D3"].font = BOLD
    for c, h in zip("DEF", ("INPUT1", "INPUT2", "DESIRED")):
        ws[f"{c}4"] = h
        ws[f"{c}4"].font = BOLD
        ws[f"{c}4"].fill = HEAD_FILL
        ws[f"{c}4"].alignment = CENTER
    for k, (a, b) in enumerate(((0, 0), (0, 1), (1, 0), (1, 1))):
        r = 5 + k
        for c, v in zip("DEF", (a, b, desired[k])):
            ws[f"{c}{r}"] = v
            ws[f"{c}{r}"].font = BLUE
            ws[f"{c}{r}"].fill = INPUT_FILL
            ws[f"{c}{r}"].alignment = CENTER
            ws[f"{c}{r}"].border = BOX

    headers = ["EPOCH", "INPUT1", "INPUT2", "W1", "W2", "BIAS", "TOPLAM", "OUTPUT", "DESIRED", "ERROR", "EPOCH HATASI"]
    for i, h in enumerate(headers):
        cell = ws.cell(FIRST - 1, 1 + i, h)
        cell.font = BOLD
        cell.fill = HEAD_FILL
        cell.alignment = CENTER
        cell.border = BOX
    ws["G10"] = "TOPLAM = W1·INPUT1 + W2·INPUT2 + BIAS;  OUTPUT = 1 eğer TOPLAM > 0, değilse 0;  ERROR = DESIRED − OUTPUT"
    ws["G10"].font = Font(name=FONT, italic=True, color="555555")

    for r in range(FIRST, last + 1):
        k = f"MOD(ROW()-{FIRST},4)+1"  # which of the four training rows
        ws[f"A{r}"] = f"=INT((ROW()-{FIRST})/4)+1"
        ws[f"B{r}"] = f"=INDEX($D$5:$D$8,{k})"
        ws[f"C{r}"] = f"=INDEX($E$5:$E$8,{k})"
        if r == FIRST:
            ws[f"D{r}"], ws[f"E{r}"], ws[f"F{r}"] = "=$B$4", "=$B$5", "=$B$6"
        else:  # weights shown in a row are the ones used for that row, i.e. after the previous row's update
            p = r - 1
            ws[f"D{r}"] = f"=D{p}+J{p}*B{p}"
            ws[f"E{r}"] = f"=E{p}+J{p}*C{p}"
            ws[f"F{r}"] = f"=F{p}+J{p}"
        ws[f"G{r}"] = f"=D{r}*B{r}+E{r}*C{r}+F{r}"
        ws[f"H{r}"] = f"=IF(G{r}>0,1,0)"
        ws[f"I{r}"] = f"=INDEX($F$5:$F$8,{k})"
        ws[f"J{r}"] = f"=I{r}-H{r}"
        if (r - FIRST) % 4 == 3:
            ws[f"K{r}"] = f"=SUMPRODUCT(ABS(J{r - 3}:J{r}))"
        for col in "ABCDEFGHIJK":
            cell = ws[f"{col}{r}"]
            cell.font = NORMAL
            cell.alignment = CENTER
            cell.border = BOX
            if ((r - FIRST) // 4) % 2 == 0:
                cell.fill = EPOCH_FILL
            if col in "DEFG":
                cell.number_format = "0.0"

    # rows with an error are highlighted
    ws.conditional_formatting.add(f"J{FIRST}:J{last}",
                                  FormulaRule(formula=[f"J{FIRST}<>0"], font=Font(name=FONT, bold=True, color="C00000")))

    # epoch summary
    ws["M11"], ws["N11"], ws["O11"] = "EPOCH", "TOPLAM HATA", "DURUM"
    for c in "MNO":
        ws[f"{c}11"].font = BOLD
        ws[f"{c}11"].fill = HEAD_FILL
        ws[f"{c}11"].alignment = CENTER
        ws[f"{c}11"].border = BOX
    for e in range(1, EPOCHS + 1):
        r = 11 + e
        a, b = FIRST + 4 * (e - 1), FIRST + 4 * e - 1
        ws[f"M{r}"] = e
        ws[f"N{r}"] = f"=SUMPRODUCT(ABS(J{a}:J{b}))"
        ws[f"O{r}"] = f'=IF(N{r}=0,"hatasız","güncelleme var")'
        for c in "MNO":
            ws[f"{c}{r}"].font = NORMAL
            ws[f"{c}{r}"].alignment = CENTER
            ws[f"{c}{r}"].border = BOX
    end = 11 + EPOCHS
    ws[f"M{end + 2}"] = "İlk hatasız epoch:"
    ws[f"M{end + 2}"].font = BOLD
    ws[f"O{end + 2}"] = f'=IFERROR(INDEX(M12:M{end},MATCH(0,N12:N{end},0)),"yok ({EPOCHS} epoch içinde)")'
    ws[f"O{end + 2}"].font = BOLD
    ws[f"M{end + 3}"] = "Öğrenilen ağırlıklar (son satırdan sonra):"
    ws[f"M{end + 3}"].font = BOLD
    ws[f"M{end + 4}"], ws[f"M{end + 5}"], ws[f"M{end + 6}"] = "W1", "W2", "BIAS"
    ws[f"O{end + 4}"] = f"=D{last}+J{last}*B{last}"
    ws[f"O{end + 5}"] = f"=E{last}+J{last}*C{last}"
    ws[f"O{end + 6}"] = f"=F{last}+J{last}"
    for r in range(end + 4, end + 7):
        ws[f"O{r}"].number_format = "0.0"
        ws[f"O{r}"].font = NORMAL

    widths = {"A": 17, "B": 9, "C": 9, "D": 9, "E": 9, "F": 9, "G": 10, "H": 9, "I": 9, "J": 8, "K": 13,
              "L": 3, "M": 9, "N": 13, "O": 22}
    for c, w in widths.items():
        ws.column_dimensions[c].width = w
    ws.freeze_panes = f"A{FIRST}"


def main():
    wb = Workbook()
    wb.remove(wb.active)
    for title, gate, start, desired, note in SHEETS:
        build_sheet(wb.create_sheet(title), gate, start, desired, note)
    OUT.parent.mkdir(parents=True, exist_ok=True)
    wb.calculation.fullCalcOnLoad = True  # Excel and LibreOffice compute every formula when the file opens
    wb.save(OUT)
    print("written", OUT)


if __name__ == "__main__":
    main()
