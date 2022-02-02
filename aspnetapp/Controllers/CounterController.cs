#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using aspnetapp;

public class CounterRequest {
    public string action { get; set; }
}
public class CounterResponse {
    public int data { get; set; }
}


/// <summary>
/// WXBizDataCrypt 的摘要说明
/// 微信小程序解密类
/// </summary>
public class WXBizDataCrypt
{
    private string _appid;
    private string _sessionKey;
 
    /// <summary>
    /// 
    /// </summary>
    /// <param name="sessionKey">sessionKey用户在小程序登录后获取的会话密钥</param>
    public WXBizDataCrypt(string sessionKey)
    {
        _appid = AppSettingUtil.AppSettings["appid"];
        _sessionKey = sessionKey;
    }
 
    /// <summary>
    /// 检验数据的真实性，并且获取解密后的明文.
    /// </summary>
    /// <param name="encryptedData">加密的用户数据</param>
    /// <param name="iv">与用户数据一同返回的初始向量</param>
    /// <param name="data">解密后的原文</param>
    /// <returns>成功0，失败返回对应的错误码</returns>
    /**
     * error code 说明.
     * <ul>
     *    <li>-41001: encodingAesKey 非法</li>
     *    <li>-41003: aes 解密失败</li>
     *    <li>-41004: 解密后得到的buffer非法</li>
     *    <li>-41005: base64加密失败</li>
     *    <li>-41016: base64解密失败</li>
     * </ul>
     */
    public int decryptData(string encryptedData, string iv, out string data)
    {
        data = string.Empty;
        if (this._sessionKey.Length != 24)
        {
            return -41001;
        }
        if (iv.Length != 24)
        {
            return -41002;
        }
        try
        {
            data = AESDecrypt(encryptedData, this._sessionKey, iv);
        }
        catch (Exception ex)
        {
            return -41004;
        }
        return 0;
    }
 
    public static string AESDecrypt(string encryptedDatatxt, string AesKey, string AesIV)
    {
        try
        {
            byte[] encryptedData = Convert.FromBase64String(encryptedDatatxt);
            RijndaelManaged rijndaelCipher = new RijndaelManaged();
            rijndaelCipher.Key = Convert.FromBase64String(AesKey);
            rijndaelCipher.IV = Convert.FromBase64String(AesIV);
            rijndaelCipher.Mode = CipherMode.CBC;
            rijndaelCipher.Padding = PaddingMode.PKCS7;
            ICryptoTransform transform = rijndaelCipher.CreateDecryptor();
            byte[] plainText = transform.TransformFinalBlock(encryptedData, 0, encryptedData.Length);
            string result = Encoding.Default.GetString(plainText);
            return result;
        }
        catch (Exception ex)
        {
            throw ex;
 
        }
    }
}

namespace aspnetapp.Controllers
{
    [Route("api/count")]
    [ApiController]
    public class CounterController : ControllerBase
    {
        private readonly CounterContext _context;

        public CounterController(CounterContext context)
        {
            _context = context;
        }
        private async Task<Counter> getCounterWithInit()
        {
            var counters = await _context.Counters.ToListAsync();
            if (counters.Count() > 0)
            {
                return counters[0];
            }
            else
            {
                var counter = new Counter { count = 0, createdAt = DateTime.Now, updatedAt = DateTime.Now };
                _context.Counters.Add(counter);
                await _context.SaveChangesAsync();
                return counter;
            }
        }
        // GET: api/count
        [HttpGet]
        public async Task<ActionResult<CounterResponse>> GetCounter()
        {
            var counter =  await getCounterWithInit();
            return new CounterResponse { data = counter.count };
        }
        
        // GET: api/count
        [HttpGet]
        public async Task<ActionResult<WXBizDataCrypt>> GetdecryptData(CounterRequest data)
        {
            string outdata = "";
            new WXBizDataCrypt("ad7a00aefcf354ecc7343fd0c130c7c1").decryptData(data.encryptedData, data.iv, out outdata);
            return new CounterResponse { data = outdata };
        }

        // POST: api/Counter
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<CounterResponse>> PostCounter(CounterRequest data)
        {
            if (data.action == "inc") {
                var counter = await getCounterWithInit();
                counter.count += 1;
                counter.updatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
                return new CounterResponse { data = counter.count };
            }
            else if (data.action == "clear") {
                var counter = await getCounterWithInit();
                counter.count = 0;
                counter.updatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
                return new CounterResponse { data = counter.count };
            }
            else {
                return BadRequest();
            }
        }
    }
}
