using System.Text;
using System.Text.Json;
using RetailCorrector;

namespace RC2FptrScript
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if(args.Length == 0)
            {
                Console.WriteLine("Синтаксис запуска утилиты: RC2FPTR (путь к папке с чеками)");
                Console.WriteLine("Нажмите на ENTER, для закрытия утилиты.");
                Console.ReadLine();
                return;
            }
            Console.WriteLine("В данный момент утилита поддерживает только ФФД 1.2!");
            Console.WriteLine("Нажмите на ENTER, для продолжения.");
            Console.ReadLine();
            var dir = Path.Combine(AppContext.BaseDirectory, "Scripts");
            var files = Directory.GetFiles(args[0]);
            if(Directory.Exists(dir)) Directory.Delete(dir);
            Directory.CreateDirectory(dir);
            foreach (var file in files)
            {
                var filename = Path.Combine(dir, $"{DateTime.Now:O}.txt");
                using var fs = File.Open(file, FileMode.Open, FileAccess.Read);
                var receipts = JsonSerializer.Deserialize(fs, ReceiptSerializationContext.Default.ReceiptArray)!;
                var text = new StringBuilder();
                foreach(var rec in receipts)
                {
                    text.AppendLine($"Fptr.setParam(1178, new Date(\"{rec.Created:yyyy'-'MM'-'dd'T'HH':'mm':'ss}\"));");
                    text.AppendLine($"Fptr.setParam(1179, \"{rec.ActNumber ?? " "}\");");
                    text.AppendLine("Fptr.utilFormTlv();");
                    text.AppendLine("correctionInfo = Fptr.getParamByteArray(Fptr.LIBFPTR_PARAM_TAG_VALUE);");
                    string op = rec.Operation switch
                    {
                        Operation.Income => "LIBFPTR_RT_SELL_CORRECTION",
                        Operation.RefundIncome => "LIBFPTR_RT_SELL_RETURN_CORRECTION",
                        Operation.Outcome => "LIBFPTR_RT_BUY_CORRECTION",
                        Operation.RefundOutcome => "LIBFPTR_RT_BUY_RETURN_CORRECTION",
                    };

                    text.AppendLine($"Fptr.setParam(Fptr.LIBFPTR_PARAM_RECEIPT_TYPE, {op});");
                    text.AppendLine("Fptr.setParam(Fptr.LIBFPTR_PARAM_RECEIPT_ELECTRONICALLY, true);");
                    text.AppendLine($"Fptr.setParam(1192, \"{rec.FiscalSign}\");");
                    text.AppendLine($"Fptr.setParam(1173, {(int)rec.CorrectionType});");
                    text.AppendLine("Fptr.setParam(1174, correctionInfo);");
                    text.AppendLine("Fptr.openReceipt();");

                    foreach(var pos in rec.Items)
                    {
                        text.AppendLine($"Fptr.setParam(Fptr.LIBFPTR_PARAM_COMMODITY_NAME, \"{pos.Name}\");");
                        text.AppendLine($"Fptr.setParam(Fptr.LIBFPTR_PARAM_PRICE, {Math.Round(pos.Price / 100.0, 2)});");
                        text.AppendLine($"Fptr.setParam(Fptr.LIBFPTR_PARAM_QUANTITY, {Math.Round(pos.Quantity / 1000.0, 3)});");
                        text.AppendLine($"Fptr.setParam(Fptr.LIBFPTR_PARAM_POSITION_SUM, {Math.Round(pos.TotalSum / 100.0, 2)});");
                        text.AppendLine($"Fptr.setParam(Fptr.LIBFPTR_PARAM_TAX_TYPE, {(int)pos.TaxRate});");
                        text.AppendLine($"Fptr.setParam(2108, {(int)pos.MeasureUnit});");
                        text.AppendLine($"Fptr.setParam(1212, {(int)pos.PosType});");
                        text.AppendLine($"Fptr.setParam(1214, {(int)pos.PayType});");
                        text.AppendLine("Fptr.registration();");
                    }
                    text.AppendLine($"Fptr.setParam(Fptr.LIBFPTR_PARAM_SUM, {Math.Round(((uint)rec.RoundedSum!) / 100.0, 2)});");
                    text.AppendLine("Fptr.receiptTotal();");
                    if(rec.Payment.Cash > 0)
                    {
                        text.AppendLine("Fptr.setParam(Fptr.LIBFPTR_PARAM_PAYMENT_TYPE, Fptr.LIBFPTR_PT_CASH);");
                        text.AppendLine($"Fptr.setParam(Fptr.LIBFPTR_PARAM_PAYMENT_SUM, {Math.Round(rec.Payment.Cash / 100.0, 2)});");
                        text.AppendLine("Fptr.payment();");
                    }
                    if(rec.Payment.ECash > 0)
                    {
                        text.AppendLine("Fptr.setParam(Fptr.LIBFPTR_PARAM_PAYMENT_TYPE, Fptr.LIBFPTR_PT_ELECTRONICALLY);");
                        text.AppendLine($"Fptr.setParam(Fptr.LIBFPTR_PARAM_PAYMENT_SUM, {Math.Round(rec.Payment.ECash / 100.0, 2)});");
                        text.AppendLine("Fptr.payment();");
                    }
                    if(rec.Payment.Pre > 0)
                    {
                        text.AppendLine("Fptr.setParam(Fptr.LIBFPTR_PARAM_PAYMENT_TYPE, Fptr.LIBFPTR_PT_PREPAID);");
                        text.AppendLine($"Fptr.setParam(Fptr.LIBFPTR_PARAM_PAYMENT_SUM, {Math.Round(rec.Payment.Pre / 100.0, 2)});");
                        text.AppendLine("Fptr.payment();");
                    }
                    if(rec.Payment.Post > 0)
                    {
                        text.AppendLine("Fptr.setParam(Fptr.LIBFPTR_PARAM_PAYMENT_TYPE, Fptr.LIBFPTR_PT_CREDIT);");
                        text.AppendLine($"Fptr.setParam(Fptr.LIBFPTR_PARAM_PAYMENT_SUM, {Math.Round(rec.Payment.Post / 100.0, 2)});");
                        text.AppendLine("Fptr.payment();");
                    }
                    if(rec.Payment.Provision > 0)
                    {
                        text.AppendLine("Fptr.setParam(Fptr.LIBFPTR_PARAM_PAYMENT_TYPE, Fptr.LIBFPTR_PT_OTHER);");
                        text.AppendLine($"Fptr.setParam(Fptr.LIBFPTR_PARAM_PAYMENT_SUM, {Math.Round(rec.Payment.Provision / 100.0, 2)});");
                        text.AppendLine("Fptr.payment();");
                    }
                    text.AppendLine("Fptr.closeReceipt();");
                    text.AppendLine();
                }
                File.WriteAllText(filename, text.ToString());
            }
        }
    }
}
