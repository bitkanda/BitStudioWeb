using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing.Text;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using bitkanda.Dal;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using Nethereum.Contracts.ContractHandlers;
using Nethereum.Hex.HexTypes;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using NethereumFaucet;
using NethereumFaucet.ViewModel;
using VerificationCode.Code;

namespace bitkanda.Controllers
{
    public class FaucetController : Controller
    {
        private readonly IHttpContextAccessor httpContextAccessor; 
        FaucetSettings settings;
        private VerificationCodeAESHelp _verificationCodeAESHelp;

        public FaucetController(IOptions<FaucetSettings> settings, 
            IHttpContextAccessor _httpContextAccessor,
            VerificationCodeAESHelp verificationCodeAESHelp)
        {
            this.settings = settings.Value;
            this.httpContextAccessor = _httpContextAccessor;
            this._verificationCodeAESHelp = verificationCodeAESHelp;
        }

        public IActionResult Index(string SourceAddress)
        {
            FaucetViewModel faucetViewModel = new FaucetViewModel
            {
                 SourceAddress= SourceAddress
            };
            //return View(faucetViewModel);
            return View(faucetViewModel);
        }
        [Function("balanceOf", "uint256")]
        public class BalanceOfFunction : FunctionMessage
        {
            [Parameter("address", "_owner", 1)]
            public string Owner { get; set; }
        }
        [Function("transfer", "bool")]
        public class TransferFunction : FunctionMessage
        {
            [Parameter("address", "_to", 1)]
            public string To { get; set; }

            [Parameter("uint256", "_value", 2)]
            public BigInteger TokenAmount { get; set; }
        }

        //check
        //private static Hashtable SendAddress = new Hashtable();

        private bool ContainsAddress(string Address, string ActivityCode = "")
        {
            using (MysqlDBContext my = new MysqlDBContext())
            {
                try
                {
                    my.Database.BeginTransaction();
                    if (my.AirDropTrans.Where(e => e.Address == Address && e.ActivityCode == ActivityCode).Count() > 0)
                    {
                        return true;
                    }

                }
                catch (Exception error)
                {
                    throw error;
                }
                finally
                {
                    my.Database.CommitTransaction();
                }

            }
            return false;
        }
        private bool AddSend(AirDropTran airs)
        {
            return AddSend(new List<AirDropTran> { airs });
        }


        private bool AddSend(List<AirDropTran> airs)
        {

            using (MysqlDBContext my = new MysqlDBContext())
            {
                try
                {
                    my.Database.BeginTransaction();
                    foreach (var air in airs)
                    {
                        if (string.IsNullOrWhiteSpace(air.ActivityCode))
                            air.ActivityCode = string.Empty;
                        my.AirDropTrans.Add(air);
                    }
                    my.SaveChanges();
                }
                catch (Exception error)
                {
                    throw error;
                }
                finally
                {

                    my.Database.CommitTransaction();
                }

            }
            return false;
        }

        private static Hashtable ips = new Hashtable();

        class Item
        {
            public List<string> Address = new List<string>();
            public long Count
            {
                get;set;
            }

            public string SourceAddress { get; set; }
        }

          

        [HttpPost]
        public async Task<ActionResult> Index(FaucetViewModel faucetViewModel)
        {
            var (_bool, _msg) = VerifyValiate();
            if (!_bool)
            {
                ModelState.AddModelError("Address", _msg);
                return View(faucetViewModel); 
            }

            string ip="";
            if (Request.Headers.ContainsKey("X-Forwarded-For"))
            {
                ip = Request.Headers["X-Forwarded-For"].ToString();
            }
            // var ip= Request.HttpContext.Connection.RemoteIpAddress.ToString();
            //  var ip= this.httpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString();

         

            if (ModelState.IsValid)
            {
                
                faucetViewModel.Address = faucetViewModel.Address.TrimStart(new char[] { '0', 'x' });
                var contractAddress = settings.ContractAddress;
                var owneraccount = new Account(settings.FunderPrivateKey);

                var balanceOfFunctionMessage = new BalanceOfFunction()
                {
                    Owner = faucetViewModel.Address,//account.Address,
                };
                var web3 = new Web3(owneraccount, settings.EthereumAddress);



                //searchBalance
                var bkdbalanceHandler = web3.Eth.GetContractQueryHandler<BalanceOfFunction>();
                var bkdbalance = await bkdbalanceHandler.QueryAsync<BigInteger>(contractAddress, balanceOfFunctionMessage);
                if (Web3.Convert.FromWei(bkdbalance) > settings.MaxAmountToFund)
                {
                    ModelState.AddModelError("address", "Account cannot be funded, already has more than " + settings.MaxAmountToFund + " BKD!");
                    return View(faucetViewModel);
                }


                if (ContainsAddress(faucetViewModel.Address))
                {
                    ModelState.AddModelError("address", "The account is being airdropped or has been airdropped.!");
                    return View(faucetViewModel);
                }

                lock (ips)
                {
                    if (ips.ContainsKey(ip))
                    {
                        Item item = ips[ip] as Item;
                        item.Address.Add(faucetViewModel.Address);
                        item.Count++;
                        ModelState.AddModelError("Address", "Please do not submit twice. We will not airdrop if you register twice");
                        return View(faucetViewModel);
                    }
                    else
                    {
                        Item item = new Item
                        {
                            SourceAddress = faucetViewModel.SourceAddress
                        };
                        item.Address.Add(faucetViewModel.Address);
                        ips.Add(ip, item);
                    }
                }
                //sendBKD

                var transactionMessage = new TransferFunction()
                {
                    FromAddress = owneraccount.Address,
                    To = faucetViewModel.Address,
                    TokenAmount = Web3.Convert.ToWei(settings.AmountToFund),

                };

                var GasPrice = await web3.Eth.GasPrice.SendRequestAsync();
                var Nonce = await web3.Eth.Transactions.GetTransactionCount.SendRequestAsync(owneraccount.Address);
                transactionMessage.GasPrice = GasPrice;
                transactionMessage.Nonce = new HexBigInteger(Nonce.Value);
                var transferHandler = web3.Eth.GetContractTransactionHandler<TransferFunction>();
                var estimate = await transferHandler.EstimateGasAsync(contractAddress, transactionMessage);
                transactionMessage.Gas = estimate.Value;

                //var ethbalance = await web3.Eth.GetBalance.SendRequestAsync(owneraccount.Address);


                string txnHash = "";
                try
                {
                    txnHash = await web3.Eth.GetContractTransactionHandler<TransferFunction>()
                          .SendRequestAsync(contractAddress, transactionMessage);
                }
                catch (Exception error)
                {
                    //amount not enough.Still successful return, but next drop.
                    var msg = "We have recorded your information, and we will airdrop it into your account next time!!!";

                    AddSend(new AirDropTran
                    {
                        AddDTM = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                        Address = faucetViewModel.Address,
                        IsSuccess = false,
                        Message = error.Message + error.StackTrace,
                        TokenAmount = settings.AmountToFund.ToString(),
                        TxnHash = string.Empty,
                        SourceAddress= faucetViewModel.SourceAddress

                    });
                    ModelState.AddModelError("Address", msg);
                    return View(faucetViewModel);
                    // add log.
                }

                AddSend(new AirDropTran
                {
                    AddDTM = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                    Address = faucetViewModel.Address,
                    IsSuccess = true,
                    Message = "",
                    TokenAmount = settings.AmountToFund.ToString(),
                    TxnHash = txnHash,
                    SourceAddress = faucetViewModel.SourceAddress
                });
                faucetViewModel.TransactionHash = txnHash;
                faucetViewModel.Amount = settings.AmountToFund;

                return View(faucetViewModel);
            }
            else
            {
                return View(faucetViewModel);
            }
        }

        public ActionResult GetIPs()
        {
            return Json(ips);
        }

        public ActionResult GetSendAddress()
        {
            //import old data.
            //System.IO.StreamReader streamReader = new System.IO.StreamReader(@"E:\开发教程\发币\新建文本文档.txt");
            //var str = streamReader.ReadToEnd();
            //var obje = Newtonsoft.Json.JsonConvert.DeserializeObject<List<DictionaryEntry>>(str);

            //List<AirDropTran> tem = new List<AirDropTran>();
            //foreach (var one in obje)
            //{
            //    var a = new AirDropTran()
            //    {
            //        ActivityCode = string.Empty,
            //        AddDTM = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
            //        Address = one.Key.ToString(),
            //        TokenAmount = settings.AmountToFund.ToString(),
            //        IsSuccess = true,
            //        TxnHash = one.Value.ToString(),

            //    };
            //    tem.Add(a);
            //}

            //AddSend(tem);


            using (MysqlDBContext my = new MysqlDBContext())
            {
                var query = from c in my.AirDropTrans
                            select c;
                return Json(query.ToList());
            }
        }

        public ActionResult DAddress(string address)
        { 
            using (MysqlDBContext my = new MysqlDBContext())
            {
                var query = from c in my.AirDropTrans
                            where c.Address== address
                            select c;
                my.AirDropTrans.RemoveRange(query);
               var r= my.SaveChanges();
                return Json(r);
            }
        }
        public ActionResult<IEnumerable<string>> Get()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"RemoteIpAddress：{httpContextAccessor.HttpContext.Connection.RemoteIpAddress}");
            if (Request.Headers.ContainsKey("X-Real-IP"))
            {
                sb.AppendLine($"X-Real-IP：{Request.Headers["X-Real-IP"].ToString()}");
            }
            if (Request.Headers.ContainsKey("X-Forwarded-For"))
            {
                sb.AppendLine($"X-Forwarded-For：{Request.Headers["X-Forwarded-For"].ToString()}");
            }
            return Ok(sb.ToString());
        }
        public ActionResult ip()
        {


            var ip = Request.HttpContext.Connection.RemoteIpAddress.ToString();
              var ip2= this.httpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString();
            return Json(new {ip=ip,ip2=ip2 });
        }

        #region code
         

        public async Task<IActionResult> VerificationCodeImage()
        {
            try
            {
                var model = await VerificationCode.Code.VerificationCodeImage.CreateHanZi();
                var json_Model = Newtonsoft.Json.JsonConvert.SerializeObject(model.point_X_Y);
                string pointBase64str = this._verificationCodeAESHelp.AES_Encrypt_Return_Base64String(json_Model);
                this._verificationCodeAESHelp.SetCookie(VerificationCodeAESHelp._YZM, pointBase64str, 5);
                string msg = "Please click in order[" + string.Join("", model.point_X_Y.Select(x => x.Word).ToList()) + "]";
                return Json(new { result = model.ImageBase64Str, msg = msg });
            }
            catch(Exception error)
            {
                return Json(new { result = "", msg =error.StackTrace+ error.Message });
            }
        }

        public IActionResult Check(string code)
        {
            try
            {
                var pointList = new List<Point_X_Y>();
                try
                {
                    pointList = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Point_X_Y>>(code);
                }
                catch (Exception)
                {
                    return Json(new { msg = "Validation failed!", status = "error" });
                }

                if (pointList.Count != 2)
                    return Json(new { msg = "Validation failed!", status = "error" });

                var _cookie = this._verificationCodeAESHelp.GetCookie(VerificationCodeAESHelp._YZM);

                if (string.IsNullOrEmpty(_cookie))
                    return Json(new { msg = "Validation failed!", status = "error" });

                string _str = this._verificationCodeAESHelp.AES_Decrypt_Return_String(_cookie);

                var _cookiesPointList = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Point_X_Y>>(_str);
                _cookiesPointList = _cookiesPointList.OrderBy(x => x.Sort).ToList();
                int i = 0;
                foreach (var item in pointList.AsParallel())
                {
                    int _x = _cookiesPointList[i]._X - item._X;
                    int _y = _cookiesPointList[i]._Y - item._Y;
                    _x = Math.Abs(_x);
                    _y = Math.Abs(_y);
                    if (_x > 25 || _y > 25)
                    {
                        return Json(new { msg = "Validation failed!", status = "error" });
                    }
                    i++;
                }

                SlideVerifyCode(true);
            }
            catch (Exception)
            {
            }

            return Json(new { msg = "Authentication is successful!", status = "ok" });
        }


        private (bool, string) VerifyValiate()
        {
            try
            {
                var _cookie = this._verificationCodeAESHelp.GetCookie(VerificationCodeAESHelp._SlideCode);
                if (string.IsNullOrEmpty(_cookie))
                {
                    SlideVerifyCode();
                    return (false, "Please Drag And Slide Verification!");
                }

                string _str = this._verificationCodeAESHelp.AES_Decrypt_Return_String(_cookie);
                var sildeCodeModel = Newtonsoft.Json.JsonConvert.DeserializeObject<SlideVerifyCodeModel>(_str);
                if (!sildeCodeModel.SlideCode)
                    return (false, "Please drag the slider and click the text!");

                var _NowTime = DateTime.Now;
                var _time = sildeCodeModel.timestamp;
                var number = (_NowTime - _time).Minutes;
                if (number > 5)
                {
                    SlideVerifyCode();
                    return (false, "The slider verification code has expired!");
                }
            }
            catch (Exception)
            {
                return (false, "Failed to scroll CAPTCHA!");
            }
            return (true, "");
        }


        private void SlideVerifyCode(bool _bool = false)
        {
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(new SlideVerifyCodeModel() { SlideCode = _bool });
            string base64Str = this._verificationCodeAESHelp.AES_Encrypt_Return_Base64String(json);
            this._verificationCodeAESHelp.SetCookie(VerificationCodeAESHelp._SlideCode, base64Str, 10);

        }

        #endregion

        public IActionResult getfont()
        {
            InstalledFontCollection fc = new InstalledFontCollection();
            var query = from c in fc.Families
                        select new
                        {
                            Name=c.Name, 
                            Path=c.ToString()
                        };

            return Json(query);
        }
    }
}