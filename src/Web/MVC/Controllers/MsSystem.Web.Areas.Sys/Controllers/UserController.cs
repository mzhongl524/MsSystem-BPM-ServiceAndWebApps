using JadeFramework.Core.Domain.Entities;
using JadeFramework.Core.Domain.Enum;
using JadeFramework.Core.Domain.Result;
using JadeFramework.Core.Extensions;
using JadeFramework.Core.Mvc;
using JadeFramework.Core.Mvc.Extensions;
using JadeFramework.Core.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using MsSystem.Utility;
using MsSystem.Utility.Filters;
using MsSystem.Web.Areas.Sys.Hubs;
using MsSystem.Web.Areas.Sys.Service;
using MsSystem.Web.Areas.Sys.ViewModel;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading.Tasks;
using SignalRMessageGroups = MsSystem.Utility.SignalRMessageGroups;

namespace MsSystem.Web.Areas.Sys.Controllers
{
    /// <summary>
    /// �û�����
    /// </summary>
    [Area("Sys")]
    public class UserController : BaseController
    {
        private readonly string AESKey = "d4ae4a06a63a4c8687a0d884cc6cdff2";

        private ISysUserService _userService;
        private ISysRoleService _roleService;
        private ISysSystemService _systemService;
        private IVerificationCode _verificationCode;
        private readonly IScanningLoginService _scanningLoginService;
        private readonly IHostingEnvironment hostingEnvironment;
        private IHubContext<ScanningLoginHub> _hubContext;

        public UserController(
            ISysUserService userService,
            ISysRoleService roleService,
            ISysSystemService systemService,
            IVerificationCode verificationCode,
            IScanningLoginService scanningLoginService,
            IServiceProvider serviceProvider,
            IHostingEnvironment hostingEnvironment)
        {
            _hubContext = serviceProvider.GetService<IHubContext<ScanningLoginHub>>();
            _userService = userService;
            _roleService = roleService;
            _systemService = systemService;
            _verificationCode = verificationCode;
            this._scanningLoginService = scanningLoginService;
            this.hostingEnvironment = hostingEnvironment;
        }

        #region �û�ҳ��

        /// <summary>
        /// �û��б�
        /// </summary>
        /// <param name="search"></param>
        /// <returns></returns>
        [HttpGet]
        [Permission]
        public async Task<IActionResult> Index([FromQuery]UserIndexSearch search)
        {
            if (search.PageIndex.IsDefault())
            {
                search.PageIndex = 1;
            }
            if (search.PageSize.IsDefault())
            {
                search.PageSize = 10;
            }
            var res = await _userService.GetUserPageAsync(search);
            return View(res);
        }

        [HttpGet]
        [Permission]
        public IActionResult Show()
        {
            return View();
        }

        /// <summary>
        /// ����Ȩ��
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Permission]
        public async Task<IActionResult> DataPrivileges()
        {
            var systems = await _systemService.ListAsync();
            return View(systems);
        }

        /// <summary>
        /// ���䲿��
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public IActionResult Dept()
        {
            return View();
        }

        #region ��¼

        /// <summary>
        /// ��¼
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login()
        {
            return View();
        }

        /// <summary>
        /// ɨ���¼
        /// </summary>
        /// <param name="account">�˻�</param>
        /// <param name="code">code</param>
        /// <returns></returns>
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ScanningLogin(string account, string code)
        {
            //�ж�code�Ƿ��Ǳ������ɣ���У���Ƿ�ʱ
            //string str = AESSecurity.AESDecrypt(code, AESKey);
            //var array = str.Split('&');
            //long ts = array[1].ToInt64();
            //if (DateTime.Now.AddMinutes(3).ToTimeStamp() > ts)
            //{
            //    return Ok("��ά��ʧЧ");
            //}
            //string qrcode = array[0];
            string qrcode = code;
            StringValues accessToken = "";
            HttpContext.Request.Headers.TryGetValue("Authorization", out accessToken);
            var res = await _scanningLoginService.ScanningLoginAsync(account, accessToken);
            if (res.LoginStatus == LoginStatus.Success)
            {
                //֪ͨҳ����ת
                var msg = SignalRMessageGroups.UserGroups.FirstOrDefault(m => m.QrCode == qrcode && m.UserId == 0);
                msg.UserId = res.User.UserId;
                msg.JSON = JsonConvert.SerializeObject(res);
                SignalRMessageGroups.Clear(qrcode, msg.UserId);
                await _hubContext.Clients.Client(msg.ConnectionId).SendAsync("HomePage", msg.QrCode);
            }
            return Ok(res);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Qr(string code)
        {
            if (code == null)
            {
                return RedirectToAction("Login");
            }
            SignalRMessageGroups.Clear();
            var msg = SignalRMessageGroups.UserGroups.FirstOrDefault(m => m.QrCode == code && m.UserId > 0);
            if (msg == null)
            {
                return RedirectToAction("Login");
            }
            await SaveLogin(JsonConvert.DeserializeObject<LoginResult<UserIdentity>>(msg.JSON));
            return Redirect("/");
        }

        /// <summary>
        /// ͼ����֤��
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public IActionResult ValidateCode()
        {
            string code = "";
            System.IO.MemoryStream ms = _verificationCode.Create(out code);
            HttpContext.Session.SetString(Constants.LoginValidateCode, code);
            Response.Body.Dispose();
            return File(ms.ToArray(), @"image/png");
        }

        /// <summary>
        /// ��¼
        /// </summary>
        /// <param name="username">�û���</param>
        /// <param name="password">����</param>
        /// <returns></returns>
        [HttpPost]
        [AllowAnonymous]
        public async Task<LoginResult<UserIdentity>> Login([FromBody]UserLoginDto model)
        {
            if (string.IsNullOrEmpty(model.username) || string.IsNullOrEmpty(model.password))
            {
                return new LoginResult<UserIdentity>
                {
                    Message = "�û�����������Ч��",
                    LoginStatus = LoginStatus.Error
                };
            }
            //if (model.validatecode.IsNullOrEmpty())
            //{
            //    return new LoginResult<UserIdentity>
            //    {
            //        Message = "��������֤�룡",
            //        LoginStatus = LoginStatus.Error
            //    };
            //}
            //if (HttpContext.Session.GetString(Constants.LoginValidateCode).ToLower() != model.validatecode.ToLower())
            //{
            //    return new LoginResult<UserIdentity>
            //    {
            //        Message = "��֤�����",
            //        LoginStatus = LoginStatus.Error
            //    };
            //}
            var loginresult = await _userService.LoginAsync(model.username, model.password);
            if (loginresult != null && loginresult.LoginStatus == LoginStatus.Success)
            {
                await SaveLogin(loginresult);
                return new LoginResult<UserIdentity>
                {
                    LoginStatus = LoginStatus.Success,
                    Message = "��¼�ɹ�"
                };
            }
            else
            {
                return new LoginResult<UserIdentity>
                {
                    Message = loginresult?.Message,
                    LoginStatus = LoginStatus.Success
                };
            }
        }

        private async Task SaveLogin(LoginResult<UserIdentity> loginResult)
        {
            ClaimsIdentity identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme);
            identity.AddClaims(loginResult.User.ToClaims());
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
        }


        #endregion

        /// <summary>
        /// �˳�
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> LogOut()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        #endregion

        #region CURD
        [HttpGet]
        [Permission("/Sys/User/Index", ButtonType.View, false)]
        [ActionName("Get")]
        public async Task<IActionResult> Get([FromQuery]long id)
        {
            var res = await _userService.GetAsync(id);
            return Ok(res);
        }

        /// <summary>
        /// ����
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        [HttpPost]
        [Permission("/Sys/User/Index", ButtonType.Add, false)]
        [ActionName("Add")]
        public async Task<IActionResult> Add([FromBody]UserShowDto dto)
        {
            dto.User.CreateUserId = UserIdentity.UserId;
            var res = await _userService.AddAsync(dto);
            return Ok(res);
        }

        /// <summary>
        /// �༭
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        [HttpPost]
        [Permission("/Sys/User/Index", ButtonType.Edit, false)]
        [ActionName("Update")]
        public async Task<IActionResult> Update([FromBody]UserShowDto dto)
        {
            dto.User.UpdateUserId = UserIdentity.UserId;
            var res = await _userService.UpdateAsync(dto);
            return Ok(res);
        }

        /// <summary>
        /// �߼�ɾ��
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        [HttpPost]
        [Permission("/Sys/User/Index", ButtonType.Delete, false)]
        [ActionName("Delete")]
        public async Task<IActionResult> Delete([FromBody]List<long> ids)
        {
            long userid = UserIdentity.UserId;
            var res = await _userService.DeleteAsync(ids, userid);
            return Ok(res);
        }
        #endregion

        #region ��ɫ����

        [HttpGet]
        [Authorize]
        [ActionName("RoleBox")]
        public async Task<IActionResult> RoleBox([Bind("userid"), FromQuery]int userid)
        {
            var res = await _roleService.GetTreeAsync(userid);
            return Ok(res);
        }

        [HttpPost]
        [Authorize]
        [ActionName("RoleBoxSave")]
        public async Task<IActionResult> RoleBoxSave([FromBody]RoleBoxDto dto)
        {
            dto.CreateUserId = UserIdentity.UserId;
            var res = await _userService.SaveUserRoleAsync(dto);
            return Ok(res);
        }

        #endregion

        #region ����Ȩ��

        [HttpGet]
        [Authorize]
        [ActionName("GetDataPrivileges")]
        public async Task<IActionResult> GetDataPrivileges([FromQuery]DataPrivilegesViewModel model)
        {
            var res = await _userService.GetPrivilegesAsync(model);
            return Ok(res);
        }

        /// <summary>
        /// ����Ȩ�ޱ���
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [Authorize]
        [ActionName("SaveDataPrivileges")]
        public async Task<IActionResult> SaveDataPrivileges([FromBody]DataPrivilegesDto model)
        {
            var res = await _userService.SaveDataPrivilegesAsync(model);
            return Ok(res);
        }

        #endregion

        #region ���ŷ���

        /// <summary>
        /// ��ȡ�û�����
        /// </summary>
        /// <param name="userid">�û�ID</param>
        /// <returns></returns>
        [HttpGet]
        [Authorize]
        [ActionName("GetUserDept")]
        public async Task<IActionResult> GetUserDept([FromQuery]long userid)
        {
            var res = await _userService.GetUserDeptAsync(userid);
            return Ok(res);
        }

        /// <summary>
        /// �����û�����
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        [HttpPost]
        [Authorize]
        [ActionName("SaveUserDept")]
        public async Task<IActionResult> SaveUserDept([FromBody]UserDeptDto dto)
        {
            var res = await _userService.SaveUserDeptAsync(dto);
            return Ok(res);
        }

        #endregion

        #region ��������

        [HttpGet]
        [Authorize]
        public IActionResult Center()
        {
            return View();
        }
        /// <summary>
        /// �û�ͷ��
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Authorize]
        public IActionResult Image()
        {
            return View();
        }
        /// <summary>
        /// �����û��ϴ��Oͷ��
        /// </summary>
        /// <param name="imgurl"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost]
        [ActionName("ModifyUserHeadImgAsync")]
        public async Task<bool> ModifyUserHeadImgAsync(string imgurl)
        {
            if (imgurl.IsNullOrEmpty())
            {
                return false;
            }
            return await _userService.ModifyUserHeadImgAsync(UserIdentity.UserId, imgurl);
        }

        /// <summary>
        /// �����ļ��ϴ�
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Authorize]
        public AjaxResult Upload()
        {
            if (Request.Form.Files.Count != 1)
            {
                return AjaxResult.Error("�ϴ�ʧ��");
            }
            var file = Request.Form.Files[0];
            var filename = ContentDispositionHeaderValue.Parse(file.ContentDisposition).FileName.Trim('"');
            string newfilename = System.Guid.NewGuid().ToString() + "." + GetFileExt(filename);
            string impath = hostingEnvironment.WebRootPath + "//uploadfile";
            if (!Directory.Exists(impath))
            {
                Directory.CreateDirectory(impath);
            }
            string newfile = impath + $@"//{newfilename}";
            using (FileStream fs = System.IO.File.Create(newfile))
            {
                file.CopyTo(fs);
                fs.Flush();
            }
            string url = "/uploadfile/" + newfilename;
            return AjaxResult.Success(data: url);
        }
        private string GetFileExt(string filename)
        {
            var array = filename.Split('.');
            int leg = array.Length;
            string ext = array[leg - 1];
            return ext;
        }



        #endregion

    }
}