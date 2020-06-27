using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Owl.Util;
using System.Xml.Linq;
using Owl.Feature;
using System.Drawing.Imaging;
using System.Drawing;

namespace Owl.BaiDu.ai
{
    public class BaiDuOCR
    {
        /// <summary>
        /// 文件名称
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 费用类别
        /// </summary>
        public string CostCategory { get; set; }
        /// <summary>
        /// 发票类别 
        /// </summary>
        public string InvoiceCategory { get; set; }
        /// <summary>
        /// 日期
        /// </summary>
        public DateTime Date { get; set; }
        /// <summary>
        /// 总金额
        /// </summary>
        public decimal TotalAmount { get; set; }
        /// <summary>
        /// 不含税金额
        /// </summary>
        public decimal Amount { get; set; }
        /// <summary>
        /// 税额
        /// </summary>
        public decimal TotalTax { get; set; }
        /// <summary>
        /// 发票种类
        /// </summary>
        public string InvoiceType { get; set; }
        /// <summary>
        /// 发票代码
        /// </summary>
        public string InvoiceCode { get; set; }
        /// <summary>
        /// 发票号码
        /// </summary>
        public string InvoiceNum { get; set; }
        /// <summary>
        /// 车牌号
        /// </summary>
        public string TaxiNum { get; set; }
        /// <summary>
        /// 时间
        /// </summary>
        public string Time { get; set; }
        /// <summary>
        /// 始发站
        /// </summary>
        public string StartStation { get; set; }
        /// <summary>
        /// 到达站
        /// </summary>
        public string DestinationStation { get; set; }
        /// <summary>
        /// 席别
        /// </summary>
        public string SeatCategory { get; set; }
        /// <summary>
        /// 乘客姓名
        /// </summary>
        public string UserName { get; set; }
        /// <summary>
        /// 车票号
        /// </summary>
        public string TicketNum { get; set; }
        /// <summary>
        /// 车次号
        /// </summary>
        public string TrainNum { get; set; }
        /// <summary>
        /// 校验码
        /// </summary>
        public string CheckCode { get; set; }
        /// <summary>
        /// 发票抬头
        /// </summary>
        public string PurchaserName { get; set; }
        /// <summary>
        /// 纳税人识别号
        /// </summary>
        public string PurchaserRegisterNum { get; set; }
        /// <summary>
        /// 地址及电话
        /// </summary>
        public string PurchaserAddress { get; set; }
        /// <summary>
        /// 开户行及账号
        /// </summary>
        public string PurchaserBank { get; set; }
        #region
        public BaiDuOCR() { }
        public BaiDuOCR(string name, string invoiceType)
        {
            Name = name;
            InvoiceType = invoiceType;
        }
        /// <summary>
        /// 增值税发票
        /// </summary>
        /// <param name="name">名称</param>
        /// <param name="date">日期</param>
        /// <param name="totalAmount">总金额</param>
        /// <param name="amount">不含税金额</param>
        /// <param name="totalTax">总税额</param>
        /// <param name="code">发票代码</param>
        /// <param name="invoiceType">发票类别</param>
        /// <param name="invoiceNum">发票号码</param>
        /// <param name="checkCode">校验码</param>
        /// <param name="purchaserName">发票抬头</param>
        /// <param name="purchaserRegisterNum">纳税人识别号</param>
        /// <param name="purchaserAddress">地址及电话</param>
        /// <param name="purchaserBank">开户行及账号</param>
        public BaiDuOCR(string name, DateTime date, decimal totalAmount,
            decimal amount, decimal totalTax, string code, string invoiceNum,
            string checkCode, string purchaserName, string purchaserRegisterNum,
            string purchaserAddress, string purchaserBank)
        {
            Name = name;
            Date = date;
            TotalAmount = totalAmount;
            Amount = amount;
            TotalTax = totalTax;
            InvoiceCode = code;
            InvoiceNum = invoiceNum;
            CheckCode = checkCode;
            PurchaserName = purchaserName;
            PurchaserRegisterNum = purchaserRegisterNum;
            PurchaserAddress = purchaserAddress;
            PurchaserBank = purchaserBank;
            InvoiceType = "增值税发票";
        }
        /// <summary>
        /// 火车票
        /// </summary> 
        /// <param name="name">名称</param>
        /// <param name="totalAmount">总金额</param>
        /// <param name="startStation">始发站</param>
        /// <param name="destStation">终点站</param>
        /// <param name="seatCategory">席别</param>
        /// <param name="userName">乘客姓名</param>
        /// <param name="ticketNum">车票号</param>
        /// <param name="trainNum">车次号</param>
        public BaiDuOCR(string name, DateTime date, decimal totalAmount,
            string startStation, string destStation, string seatCategory,
            string userName, string ticketNum, string trainNum)
        {
            Name = name;
            Date = date;
            TotalAmount = totalAmount;
            Amount = totalAmount;
            StartStation = startStation;
            DestinationStation = destStation;
            SeatCategory = seatCategory;
            UserName = userName;
            TicketNum = ticketNum;
            TrainNum = trainNum;
            InvoiceType = "火车票";
        }
        /// <summary>
        /// 出租车
        /// </summary>
        /// <param name="name">名称</param>
        /// <param name="date">日期</param>
        /// <param name="totalAmount">总金额</param>
        /// <param name="code">发票代码</param>
        /// <param name="invoiceNum">发票号码</param>
        /// <param name="taxiNum">车牌号</param>
        /// <param name="time">上下车时间</param>
        public BaiDuOCR(string name, DateTime date, decimal totalAmount,
            string code, string invoiceNum, string taxiNum, string time)
        {
            Name = name;
            Date = date;
            TotalAmount = totalAmount;
            Amount = totalAmount;
            InvoiceCode = code;
            InvoiceNum = invoiceNum;
            TaxiNum = taxiNum;
            Time = time;
            InvoiceType = "出租车票";
        }
        /// <summary>
        /// 通用机打发票
        /// </summary>
        /// <param name="name">名称</param>
        /// <param name="date">日期</param>
        /// <param name="totalAmount">总金额</param>
        /// <param name="code">发票代码</param>
        /// <param name="invoiceNum">发票号码</param>
        public BaiDuOCR(string name, DateTime date, decimal totalAmount,
            string code, string invoiceNum)
        {
            Name = name;
            Date = date;
            TotalAmount = totalAmount;
            Amount = totalAmount;
            InvoiceCode = code;
            InvoiceNum = invoiceNum;
            InvoiceType = "机打发票";
        }
        /// <summary>
        /// 定额发票
        /// </summary>
        /// <param name="name">名称</param>
        /// <param name="totalAmount">总金额</param>
        /// <param name="code">发票代码</param>
        /// <param name="invoiceNum">发票号码</param>
        public BaiDuOCR(string name, decimal totalAmount, string code, string invoiceNum)
        {
            Name = name;
            TotalAmount = totalAmount;
            Amount = totalAmount;
            InvoiceCode = code;
            InvoiceNum = invoiceNum;
            InvoiceType = "定额发票";
        }
        ////行程单
        //public BaiDuOCR(string name, DateTime date, decimal totalAmount,
        //    string code, string num)
        //{
        //}
        #endregion

        public List<BaiDuOCR> GetbaiDuIOCRs(string filePath)
        {
            //解析百度返回的数据，返回集合
            string base64 = getFileBase64(filePath);
            var filename = Path.GetFileName(filePath);
            return GetbaiDuIOCRs(filename, base64);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public List<BaiDuOCR> GetbaiDuIOCRs(string filename, byte[] buffer)
        {
            //byte[]转成 Base64 形式的String
            string base64 = Convert.ToBase64String(buffer);
            return GetbaiDuIOCRs(filename, base64);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="base64"></param>
        /// <returns></returns>
        public List<BaiDuOCR> GetbaiDuIOCRs(string filename, string base64)
        {
            //解析百度返回的数据，返回集合
            string result = postBaiduIOCR(base64);
            //Encoding ei = Encoding.Default;
            //string result = File.ReadAllText("d:\\a.txt", ei);

            var root = JObject.Parse(result);
            var words = root.GetValue("data");
            var invoices = words["ret"].Children();
            List<BaiDuOCR> baiDuOCRs = new List<BaiDuOCR>();
            foreach (var invoice in invoices)
            {
                BaiDuOCR baiDuOCR = new BaiDuOCR();
                baiDuOCR.Name = filename;
                var error_msg = invoice["error_msg"].ToString2();
                var templateSign = invoice["templateSign"].ToString2();
                if (!string.IsNullOrEmpty(error_msg) || templateSign == "others")
                {
                    baiDuOCRs.Add(baiDuOCR);
                    continue;
                }
                var rets = invoice["ret"].Children();
                switch (templateSign)
                {
                    case "vat_invoice": //增值税发票
                        foreach (var word in rets)
                        {
                            if (word["word_name"].ToString2() == "InvoiceType")
                                baiDuOCR.InvoiceType = word["word"].ToString2();
                            else if (word["word_name"].ToString2() == "InvoiceDate")
                                baiDuOCR.Date = word["word"].ToTryDateTime();
                            else if (word["word_name"].ToString2() == "AmountInFiguers")
                                baiDuOCR.TotalAmount = word["word"].ToDecimal();
                            else if (word["word_name"].ToString2() == "TotalAmount")
                                baiDuOCR.Amount = word["word"].ToDecimal();
                            else if (word["word_name"].ToString2() == "TotalTax")
                                baiDuOCR.TotalTax = word["word"].ToDecimal();
                            else if (word["word_name"].ToString2() == "InvoiceCode")
                                baiDuOCR.InvoiceCode = word["word"].ToString2();
                            else if (word["word_name"].ToString2() == "InvoiceNum")
                                baiDuOCR.InvoiceNum = word["word"].ToString2();
                            else if (word["word_name"].ToString2() == "CheckCode")
                                baiDuOCR.CheckCode = word["word"].ToString2();
                            else if (word["word_name"].ToString2() == "PurchaserName")
                                baiDuOCR.PurchaserName = word["word"].ToString2();
                            else if (word["word_name"].ToString2() == "PurchaserRegisterNum")
                                baiDuOCR.PurchaserRegisterNum = word["word"].ToString2();
                            else if (word["word_name"].ToString2() == "PurchaserAddress")
                                baiDuOCR.PurchaserAddress = word["word"].ToString2();
                            else if (word["word_name"].ToString2() == "PurchaserBank")
                                baiDuOCR.PurchaserBank = word["word"].ToString2();
                        }
                        baiDuOCRs.Add(baiDuOCR);
                        break;
                    case "taxi": //出租车票
                        foreach (var word in rets)
                        {
                            if (word["word_name"].ToString2() == "Date")
                                baiDuOCR.Date = word["word"].ToTryDateTime();
                            else if (word["word_name"].ToString2() == "Fare")
                                baiDuOCR.TotalAmount = word["word"].ToString2().Replace("¥", "").Replace("元", "").ToDecimal();
                            else if (word["word_name"].ToString2() == "TaxiNum")
                                baiDuOCR.TaxiNum = word["word"].ToString2();
                            else if (word["word_name"].ToString2() == "InvoiceCode")
                                baiDuOCR.InvoiceCode = word["word"].ToString2();
                            else if (word["word_name"].ToString2() == "InvoiceNum")
                                baiDuOCR.InvoiceNum = word["word"].ToString2();
                        }
                        baiDuOCR.InvoiceType = "出租车票";
                        baiDuOCRs.Add(baiDuOCR);
                        break;
                    case "roll_ticket":  //卷票
                        foreach (var word in rets)
                        {
                            if (word["word_name"].ToString2() == "InvoiceType")
                                baiDuOCR.InvoiceType = word["word"].ToString2();
                            else if (word["word_name"].ToString2() == "InvoiceDate")
                                baiDuOCR.Date = word["word"].ToTryDateTime();
                            else if (word["word_name"].ToString2() == "AmountInFiguers")
                                baiDuOCR.TotalAmount = word["word"].ToDecimal();
                            else if (word["word_name"].ToString2() == "TotalAmount")
                                baiDuOCR.Amount = word["word"].ToDecimal();
                            else if (word["word_name"].ToString2() == "TotalTax")
                                baiDuOCR.TotalTax = word["word"].ToDecimal();
                            else if (word["word_name"].ToString2() == "InvoiceCode")
                                baiDuOCR.InvoiceCode = word["word"].ToString2();
                            else if (word["word_name"].ToString2() == "InvoiceNum")
                                baiDuOCR.InvoiceNum = word["word"].ToString2();
                            else if (word["word_name"].ToString2() == "CheckCode")
                                baiDuOCR.CheckCode = word["word"].ToString2();
                            else if (word["word_name"].ToString2() == "PurchaserName")
                                baiDuOCR.PurchaserName = word["word"].ToString2();
                            else if (word["word_name"].ToString2() == "PurchaserRegisterNum")
                                baiDuOCR.PurchaserRegisterNum = word["word"].ToString2();
                            else if (word["word_name"].ToString2() == "PurchaserAddress")
                                baiDuOCR.PurchaserAddress = word["word"].ToString2();
                            else if (word["word_name"].ToString2() == "PurchaserBank")
                                baiDuOCR.PurchaserBank = word["word"].ToString2();
                        }
                        baiDuOCRs.Add(baiDuOCR);
                        break;
                    case "train_ticket":  //火车票
                        foreach (var word in rets)
                        {
                            if (word["word_name"].ToString2() == "name")
                                baiDuOCR.UserName = word["word"].ToString2();
                            else if (word["word_name"].ToString2() == "date")
                                baiDuOCR.Date = word["word"].ToTryDateTime();
                            else if (word["word_name"].ToString2() == "seat_category")
                                baiDuOCR.SeatCategory = word["word"].ToString2();
                            else if (word["word_name"].ToString2() == "starting_station")
                                baiDuOCR.StartStation = word["word"].ToString2();
                            else if (word["word_name"].ToString2() == "ticket_num")
                                baiDuOCR.TicketNum = word["word"].ToString2();
                            else if (word["word_name"].ToString2() == "ticket_rates")
                                baiDuOCR.TotalAmount = word["word"].ToString2().Replace("￥", "").Replace("元", "").ToDecimal();
                            else if (word["word_name"].ToString2() == "destination_station")
                                baiDuOCR.DestinationStation = word["word"].ToString2();

                            else if (word["word_name"].ToString2() == "train_num")
                                baiDuOCR.TrainNum = word["word"].ToString2();
                            else if (word["word_name"].ToString2() == "seat_num")
                                baiDuOCR.TrainNum = word["word"].ToString2();
                        }
                        var train_num = rets.FirstOrDefault(t => t["word_name"].ToString2() == "train_num");
                        var seat_num = rets.FirstOrDefault(t => t["word_name"].ToString2() == "seat_num");
                        baiDuOCR.TrainNum = string.Format("{0} {1}", train_num["word"], seat_num["word"]);
                        baiDuOCR.InvoiceType = "火车票";
                        baiDuOCRs.Add(baiDuOCR);
                        break;
                    case "quota_invoice":  //定额发票
                        foreach (var word in rets)
                        {
                            if (word["word_name"].ToString2() == "invoice_code")
                                baiDuOCR.InvoiceCode = word["word"].ToString2();
                            else if (word["word_name"].ToString2() == "invoice_rate")
                                baiDuOCR.TotalAmount = word["word"].ToString2().ToDouble().ToDecimal();
                            else if (word["word_name"].ToString2() == "invoice_number")
                                baiDuOCR.InvoiceNum = word["word"].ToString2();
                        }
                        baiDuOCR.InvoiceType = "定额发票";
                        baiDuOCRs.Add(baiDuOCR);
                        break;
                    case "travel_itinerary":  //行程单
                        foreach (var word in rets)
                        {
                            if (word["word_name"].ToString2() == "invoice_code")
                                baiDuOCR.InvoiceCode = word["word"].ToString2();
                            else if (word["word_name"].ToString2() == "invoice_rate")
                                baiDuOCR.TotalAmount = word["word"].ToString2().ToDouble().ToDecimal();
                            else if (word["word_name"].ToString2() == "invoice_number")
                                baiDuOCR.InvoiceNum = word["word"].ToString2();
                        }
                        baiDuOCR.InvoiceType = "行程单";
                        baiDuOCRs.Add(baiDuOCR);
                        break;
                    case "printed_invoice":  //机打发票
                        foreach (var word in rets)
                        {
                            if (word["word_name"].ToString2() == "TotalTax")
                                baiDuOCR.TotalTax = word["word"].ToDouble().ToDecimal();
                            //else if (word["word_name"].ToString2() == "InvoiceCode")
                            //    baiDuOCR.TotalAmount = word["word"].ToDouble().ToDecimal();
                            //else if (word["word_name"].ToString2() == "InvoiceDate")
                            //    baiDuOCR.Date = word["word"].ToTryDateTime();
                            //else if (word["word_name"].ToString2() == "CommodityName")
                            //    baiDuOCR.InvoiceNum = word["word"].ToString2();
                            //else if (word["word_name"].ToString2() == "InvoiceNum")
                            //    baiDuOCR.InvoiceNum = word["word"].ToString2();
                            //else if (word["word_name"].ToString2() == "InvoiceType")
                            //    baiDuOCR.InvoiceNum = word["word"].ToString2();
                        }
                        baiDuOCR.InvoiceType = "机打发票";
                        baiDuOCRs.Add(baiDuOCR);
                        break;
                    default:
                        baiDuOCRs.Add(baiDuOCR);
                        break;
                }
                if (baiDuOCR.TotalAmount == 0 && baiDuOCR.Amount != 0)
                    baiDuOCR.TotalAmount = baiDuOCR.Amount + baiDuOCR.TotalTax;
                else if (baiDuOCR.Amount == 0 && baiDuOCR.TotalAmount != 0)
                    baiDuOCR.Amount = baiDuOCR.TotalAmount;
            }
            return baiDuOCRs;
        }

        /// <summary>
        /// 请求百度，获取识别结果
        /// </summary>
        /// <param name="filename">文件名</param>
        /// <param name="base64"></param>
        /// <returns></returns>
        public List<BaiDuOCR> GetbaiDuOCRs(string filename, byte[] buffer, string imgClass = "o2o")
        {
            string base64 = Convert.ToBase64String(buffer);
            return GetbaiDuOCRs(filename, base64, imgClass);
        }
        /// <summary>
        /// 请求百度，获取识别结果
        /// </summary>
        /// <param name="filePath">文件地址</param>
        /// <returns></returns>
        public List<BaiDuOCR> GetbaiDuOCRs(string filePath, string imgClass = "o2o")
        {
            string baser64 = getFileBase64(filePath);
            String str = "image=" + HttpUtility.UrlEncode(baser64);
            byte[] buffer = getFileByte(str);
            var filename = Path.GetFileName(filePath);
            return GetbaiDuOCRs(filename, buffer, imgClass);
        }

        #region 图片压缩 
        public byte[] CompressImage(byte[] buffer, int size = 4, int quality = 90)
        {
            string img_base64 = Convert.ToBase64String(buffer);
            var base64 = CompressImage(null, img_base64, size, quality);
            return getFileByte(base64);
        }
        public string CompressImage(string filePath, string img_base64, int size = 4, int quality = 90)
        {
            Image bmp = null;
            bool flag = false;
            if (string.IsNullOrEmpty(img_base64))
            {
                bmp = Image.FromFile(filePath);
                FileInfo firstFileInfo = new FileInfo(filePath);
                if (firstFileInfo.Length > size * 1024 * 1024)
                    flag = true;
            }
            else
            {
                byte[] arr = Convert.FromBase64String(img_base64);
                MemoryStream ms = new MemoryStream(arr);
                bmp = new Bitmap(ms);
                int strLength = img_base64.Length;// 原来的字符流大小，单位为字节
                int imgSize = (strLength - (strLength / 8) * 2) / 1024 / 1024; //计算后得到的文件流大小，单位为 M
                if (imgSize > size)
                    flag = true;
            }
            if (bmp.Width > 4096 || bmp.Height > 4096)
                flag = true;
            if (flag)
            {
                return CompressImage(bmp, size, quality);
            }
            else
                return img_base64;
        }
        /// <summary>
        /// 无损压缩图片
        /// </summary>
        /// <param name="iSource"></param> 
        /// <param name="size">压缩后图片的最大大小</param> 
        /// <param name="quality">压缩质量（数字越小压缩率越高）1-100</param>
        /// <returns></returns>
        public string CompressImage(Image iSource, int size = 4, int quality = 90)
        {
            ImageFormat tFormat = iSource.RawFormat;
            int dHeight = iSource.Height / 2;
            int dWidth = iSource.Width / 2;
            int sW = 0, sH = 0;
            //按比例缩放
            Size tem_size = new Size(iSource.Width, iSource.Height);
            if (tem_size.Width > dHeight || tem_size.Width > dWidth)
            {
                if ((tem_size.Width * dHeight) > (tem_size.Width * dWidth))
                {
                    sW = dWidth;
                    sH = (dWidth * tem_size.Height) / tem_size.Width;
                }
                else
                {
                    sH = dHeight;
                    sW = (tem_size.Width * dHeight) / tem_size.Height;
                }
            }
            else
            {
                sW = tem_size.Width;
                sH = tem_size.Height;
            }

            Bitmap ob = new Bitmap(dWidth, dHeight);
            Graphics g = Graphics.FromImage(ob);

            g.Clear(Color.WhiteSmoke);
            g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

            g.DrawImage(iSource, new Rectangle((dWidth - sW) / 2, (dHeight - sH) / 2, sW, sH), 0, 0, iSource.Width, iSource.Height, GraphicsUnit.Pixel);

            g.Dispose();

            //以下代码为保存图片时，设置压缩质量
            EncoderParameters ep = new EncoderParameters();
            long[] qy = new long[1];
            qy[0] = quality;//设置压缩的比例1-100
            EncoderParameter eParam = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, qy);
            ep.Param[0] = eParam;

            try
            {
                ImageCodecInfo[] arrayICI = ImageCodecInfo.GetImageEncoders();
                ImageCodecInfo jpegICIinfo = null;
                for (int x = 0; x < arrayICI.Length; x++)
                {
                    if (arrayICI[x].FormatDescription.Equals("JPEG"))
                    {
                        jpegICIinfo = arrayICI[x];
                        break;
                    }
                }
                if (jpegICIinfo != null)
                {
                    MemoryStream ms = new MemoryStream();
                    ob.Save(ms, jpegICIinfo, ep);
                    ob.Save("d:\\aaaaaa.jpg", jpegICIinfo, ep);
                    byte[] arr = new byte[ms.Length];
                    ms.Position = 0;
                    ms.Read(arr, 0, (int)ms.Length);
                    ms.Close();
                    return Convert.ToBase64String(arr);
                }
                else
                {
                    MemoryStream ms = new MemoryStream();
                    ob.Save(ms, tFormat);
                    byte[] arr = new byte[ms.Length];
                    ms.Position = 0;
                    ms.Read(arr, 0, (int)ms.Length);
                    ms.Close();
                    return Convert.ToBase64String(arr);
                }
            }
            catch
            {
                MemoryStream ms = new MemoryStream();
                iSource.Save(ms, tFormat);
                byte[] arr = new byte[ms.Length];
                ms.Position = 0;
                ms.Read(arr, 0, (int)ms.Length);
                ms.Close();
                return Convert.ToBase64String(arr);
            }
            finally
            {
                iSource.Dispose();
                ob.Dispose();
            }
        }

        #endregion

        /// <summary>
        /// 请求百度，获取识别结果
        /// </summary>
        /// <param name="filename">文件名</param>
        /// <param name="buffer">图片字节</param>
        /// <returns></returns>
        public List<BaiDuOCR> GetbaiDuOCRs(string filename, string img_base64, string imgClass = "o2o")
        {
            //判断图片大小，如果大于 3M ，则进行压缩 
            img_base64 = CompressImage(null, img_base64);
            if (imgClass == "o2m")
                return GetbaiDuIOCRs(filename, img_base64);
            var base64 = img_base64;
            if (!base64.StartsWith("image="))
                base64 = "image=" + HttpUtility.UrlEncode(base64);
            byte[] buffer = getFileByte(base64);
            List<BaiDuOCR> baiDuOCRs = new List<BaiDuOCR>();
            try
            {
                #region  普通发票识别接口
                //调用通用识别接口，判断需要走哪个接口
                string result = postBaidu("accurate_basic", buffer);
                //根据result里的信息，判断当前是什么票据，然后进行调用对应的接口
                if ((result.Contains("增值税") || result.Contains("普通发票") || result.Contains("专用发票")
                    || (result.Contains("增") && result.Contains("发票"))))
                {
                    if (result.Contains("卷票"))
                    {
                        base64 = base64 + "&type=roll";
                        buffer = getFileByte(base64);
                    }
                    var root = JObject.Parse(postBaidu("vat_invoice", buffer));
                    if (root.GetValue("error_code") == null)
                    {
                        var words = root.GetValue("words_result");
                        //var invoiceType = words["InvoiceType"].ToString2();
                        var date = words["InvoiceDate"].ToString2().ToTryDateTime();
                        var totalAmount = words["AmountInFiguers"].ToDecimal();
                        var amount = words["TotalAmount"].ToString2().ToDecimal();
                        var totalTax = words["TotalTax"].ToString2().ToDecimal();
                        var code = words["InvoiceCode"].ToString2();
                        var invoiceNum = words["InvoiceNum"].ToString2();
                        var checkCode = words["CheckCode"].ToString2();
                        var purchaserName = words["PurchaserName"].ToString2();
                        var purchaserRegisterNum = words["PurchaserRegisterNum"].ToString2();
                        var purchaserAddress = words["PurchaserAddress"].ToString2();
                        var purchaserBank = words["PurchaserBank"].ToString2();
                        baiDuOCRs.Add(new BaiDuOCR(filename, date, totalAmount, amount, totalTax, code, invoiceNum, checkCode, purchaserName, purchaserRegisterNum, purchaserAddress, purchaserBank));
                    }
                    else
                    {
                        baiDuOCRs.Add(new BaiDuOCR(filename, "增值税发票"));
                    }
                }
                else if (result.Contains("定额发票") || result.Contains("定额") || (result.Contains("定") && result.Contains("发票")))
                {
                    var root = JObject.Parse(postBaidu("quota_invoice", buffer));
                    if (root.GetValue("error_code") == null)
                    {
                        var words = root.GetValue("words_result");
                        var totalAmount = words["invoice_rate"].ToDouble().ToDecimal();
                        var invoiceCode = words["invoice_code"].ToString2();
                        var invoiceNumber = words["invoice_number"].ToString2();
                        baiDuOCRs.Add(new BaiDuOCR(filename, totalAmount, invoiceCode, invoiceNumber));
                    }
                    else
                    {
                        baiDuOCRs.Add(new BaiDuOCR(filename, "定额发票"));
                    }
                }
                else if (result.Contains("通用机打发票"))
                {
                    var root = JObject.Parse(postBaidu("invoice", buffer));
                    if (root.GetValue("error_code") == null)
                    {
                        var words = root.GetValue("words_result");
                        var totalAmount = words["CommodityName"].ToDecimal();
                        var invoiceCode = words["InvoiceCode"].ToString2();
                        var invoiceNumber = words["InvoiceDate"].ToString2();
                        //var invoiceNumber = words["InvoiceNum"].ToString();
                        //var invoiceNumber = words["InvoiceType"].ToString();
                        //var totalAmount = words["TotalTax"].ToString();
                        baiDuOCRs.Add(new BaiDuOCR(filename, totalAmount, invoiceCode, invoiceNumber));
                    }
                    else
                    {
                        baiDuOCRs.Add(new BaiDuOCR(filename, "通用机打发票"));
                    }
                }
                else if (result.Contains("12306") || result.Contains("中国铁路"))
                {
                    var root = JObject.Parse(postBaidu("train_ticket", buffer));
                    if (root.GetValue("error_code") == null)
                    {
                        var words = root.GetValue("words_result");
                        var userName = words["name"].ToString2();
                        var date = words["date"].ToString().ToTryDateTime();
                        var destStation = words["destination_station"].ToString2();
                        var startStation = words["starting_station"].ToString2();
                        var trainNum = words["train_num"].ToString2();
                        var seatNum = words["seat_num"].ToString2();
                        var seatCategory = words["seat_category"].ToString2();
                        var ticketNum = words["ticket_num"].ToString2();
                        var totalAmount = words["ticket_rates"].ToString2().Replace("￥", "").Replace("元", "").ToDecimal();
                        baiDuOCRs.Add(new BaiDuOCR(filename, date, totalAmount, startStation, destStation, seatCategory, userName, ticketNum, string.Format("{0} {1}", trainNum, seatNum)));
                    }
                    else
                    {
                        baiDuOCRs.Add(new BaiDuOCR(filename, "火车票"));
                    }
                }
                else if (result.Contains("出租车") || result.Contains("TAXI"))
                {
                    var root = JObject.Parse(postBaidu("taxi_receipt", buffer));
                    if (root.GetValue("error_code") == null)
                    {
                        var words = root.GetValue("words_result");
                        var date = words["Date"].ToString2().ToTryDateTime();
                        var totalAmount = words["Fare"].ToString2().Replace("¥", "").Replace("元", "").ToDecimal();
                        var taxiNum = words["TaxiNum"].ToString2();
                        var code = words["InvoiceCode"].ToString2();
                        var invoiceNum = words["InvoiceNum"].ToString2();
                        var time = words["Time"].ToString2();
                        baiDuOCRs.Add(new BaiDuOCR(filename, date, totalAmount, code, invoiceNum, taxiNum, time));
                    }
                    else
                    {
                        baiDuOCRs.Add(new BaiDuOCR(filename, "出租车票"));
                    }
                }
                else if (result.Contains("行程单"))
                {
                    var o = JObject.Parse(postBaidu("air_ticket", buffer));

                }
                else
                {
                    return GetbaiDuIOCRs(filename, img_base64);
                }

                #endregion
            }
            catch (Exception e)
            {
            }
            return baiDuOCRs;
        }
        #region 获取图片64位编码
        /// <summary>
        /// 获取图片64位编码
        /// </summary>
        /// <param name="fileName">图片地址</param>
        /// <returns></returns>
        static String getFileBase64(String fileName)
        {
            FileStream filestream = new FileStream(fileName, FileMode.Open);
            byte[] arr = new byte[filestream.Length];
            filestream.Read(arr, 0, (int)filestream.Length);
            string baser64 = Convert.ToBase64String(arr);
            filestream.Close();
            return baser64;
        }
        #endregion
        #region 获取图片字节
        /// <summary>
        /// 获取图片字节
        /// </summary>
        /// <param name="Base64">图片</param>
        /// <returns></returns>
        static byte[] getFileByte(string Base64)
        {
            Encoding encoding = Encoding.Default;
            byte[] buffer = encoding.GetBytes(Base64);
            return buffer;
        }
        #endregion
        #region 请求百度
        /// <summary>
        /// 请求百度
        /// </summary>
        /// <param name="postType">请求类型</param>
        /// <param name="buffer">图片字节信息流</param>
        /// <returns></returns>
        static string postBaidu(string postType, byte[] buffer)
        {
            string token = GetAccessToken();
            string host = string.Format("https://aip.baidubce.com/rest/2.0/ocr/v1/{0}?access_token={1}&detect_direction=true", postType, token);
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(host);
            request.Method = "post";
            request.KeepAlive = true;
            request.ContentLength = buffer.Length;
            request.GetRequestStream().Write(buffer, 0, buffer.Length);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
            string result = reader.ReadToEnd();
            return result;
        }
        #endregion
        #region 百度 财会版
        /// <summary>
        /// 财会版 -识别混帖发票
        /// </summary>
        /// <param name="image_b64">图片base64编码</param>
        static string postBaiduIOCR(string image_b64)
        {
            string result = null;
            // iocr识别api_url
            String recognise_api_url = "https://aip.baidubce.com/rest/2.0/solution/v1/iocr/recognise/finance";
            string token = GetAccessToken();
            // iocr混贴票据识别的请求bodys
            int detectorId = 0;
            String detector_bodys = "access_token=" + token + "&detectorId=" + detectorId + "&image=" + HttpUtility.UrlEncode(image_b64);
            HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(recognise_api_url);
            httpWebRequest.ContentType = "application/x-www-form-urlencoded";
            httpWebRequest.Method = "POST";
            httpWebRequest.KeepAlive = true;
            try
            {
                // 请求混贴票据识别
                byte[] btBodys = Encoding.UTF8.GetBytes(detector_bodys);
                httpWebRequest.ContentLength = btBodys.Length;
                httpWebRequest.GetRequestStream().Write(btBodys, 0, btBodys.Length);
                HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                StreamReader streamReader = new StreamReader(httpWebResponse.GetResponseStream());
                string responseContent = streamReader.ReadToEnd();
                httpWebResponse.Close();
                streamReader.Close();
                httpWebRequest.Abort();
                httpWebResponse.Close();
                result = responseContent;
            }
            catch (Exception e)
            {
            }
            return result;
        }
        #endregion
        #region 获取百度token
        // 调用getAccessToken()获取的 access_token建议根据expires_in 时间 设置缓存 
        // 百度云中开通对应服务应用的 API Key 建议开通应用的时候多选服务
        private static string clientId = DbSfConfig.GetSetting("baiduocr", "clientId");
        // 百度云中开通对应服务应用的 Secret Key
        private static string clientSecret = DbSfConfig.GetSetting("baiduocr", "clientSecret");

        /// <summary>
        /// 获取百度token
        /// </summary>
        /// <returns></returns> 
        static string GetAccessToken()
        {
            AccessToken accessToken = new AccessToken();
            string key = string.Format("bd.api.ticket.{0}.{1}", clientId, clientSecret);
            accessToken = Cache.Session<AccessToken>(key);
            if (accessToken == null || accessToken.Deadline < DateTime.Now)
            {
                accessToken = getToken();
                Cache.Session(key, accessToken);
            }
            return accessToken.Value;
        }
        static AccessToken getToken()
        {
            AccessToken accessToken = new AccessToken();
            string xmlPath = System.AppDomain.CurrentDomain.BaseDirectory;
            string file = xmlPath + "\\baidu.xml";
            if (File.Exists(file))
            {
                //将XML文件加载进来
                XDocument document = XDocument.Load(file);
                //获取到XML的根元素进行操作
                XElement root = document.Root;
                XElement ele = root.Element("TOKEN");
                //获取name标签的值
                XElement deadline = ele.Element("deadline");
                accessToken.Deadline = deadline.Value.ToDateTime();
                XElement val = ele.Element("value");
                accessToken.Value = val.Value;
                //将读取的值写入对象，判断是否已过期
                if (accessToken.Deadline < DateTime.Now) //已过期
                {
                    return getTokenByBaidu(file);
                }
            }
            else
            {
                accessToken = getTokenByBaidu(file);
            }
            return accessToken;
        }
        static AccessToken getTokenByBaidu(string xmlPath)
        {
            AccessToken accessToken = new AccessToken();
            String authHost = "https://aip.baidubce.com/oauth/2.0/token";
            HttpClient client = new HttpClient();
            List<KeyValuePair<String, String>> paraList = new List<KeyValuePair<string, string>>();
            paraList.Add(new KeyValuePair<string, string>("grant_type", "client_credentials"));
            paraList.Add(new KeyValuePair<string, string>("client_id", clientId));
            paraList.Add(new KeyValuePair<string, string>("client_secret", clientSecret));
            HttpResponseMessage response = client.PostAsync(authHost, new FormUrlEncodedContent(paraList)).Result;
            string result = response.Content.ReadAsStringAsync().Result;
            //{
            //    "refresh_token": "25.b55fe1d287227ca97aab219bb249b8ab.315360000.1798284651.282335-8574074",
            //    "expires_in": 2592000,
            //    "scope": "public wise_adapt",
            //    "session_key": "9mzdDZXu3dENdFZQurfg0Vz8slgSgvvOAUebNFzyzcpQ5EnbxbF+hfG9DQkpUVQdh4p6HbQcAiz5RmuBAja1JJGgIdJI",
            //    "access_token": "24.6c5e1ff107f0e8bcef8c46d3424a0e78.2592000.1485516651.282335-8574074",
            //    "session_secret": "dfac94a3489fe9fca7c3221cbf7525ff"
            //}

            //错误的信息
            //{
            //    "error": "invalid_client",
            //    "error_description": "unknown client id"
            //}

            var tiketDic = result.DeJson<Dictionary<string, object>>();
            if (tiketDic.ContainsKey("expires_in"))
            {
                accessToken.Deadline = DateTime.Now.AddSeconds(tiketDic["expires_in"].ToInt() - 100);
                accessToken.Value = tiketDic["access_token"].ToString2();
                if (File.Exists(xmlPath))
                { //将XML文件加载进来
                    XDocument document = XDocument.Load(xmlPath);
                    //获取到XML的根元素进行操作
                    XElement root = document.Root;
                    XElement ele = root.Element("TOKEN");
                    //获取name标签的值
                    XElement deadline = ele.Element("deadline");
                    deadline.Value = accessToken.Deadline.ToString("yyyy/MM/dd HH:mm:ss");
                    XElement val = ele.Element("value");
                    val.Value = accessToken.Value;
                    root.Save(xmlPath);
                }
                else
                {
                    //获取根节点对象
                    XDocument document = new XDocument();
                    XElement root = new XElement("BaiDu");
                    XElement book = new XElement("TOKEN");
                    book.SetElementValue("deadline", accessToken.Deadline.ToString("yyyy/MM/dd HH:mm:ss"));
                    book.SetElementValue("value", accessToken.Value);
                    root.Add(book);
                    root.Save(xmlPath);
                }
            }
            else
                return null;
            return accessToken;
        }
        #endregion
    }
    public class AccessToken
    {
        /// <summary>
        /// Token 值
        /// </summary>
        public string Value { get; set; }
        /// <summary>
        /// 有效期截止时间，(赋值的时候，需要减少100秒钟，防止出现时间差)
        /// </summary>
        public DateTime Deadline { get; set; }
    }
}
