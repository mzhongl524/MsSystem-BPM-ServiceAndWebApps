using JadeFramework.Core.Extensions;
using Microsoft.AspNetCore.Mvc;
using MsSystem.Web.Areas.Sys.ViewModel;
using MsSystem.Web.Areas.Sys.Service;
using System.Threading.Tasks;

namespace MsSystem.Web.Areas.Sys.Controllers
{
    /// <summary>
    /// ���
    /// </summary>
    [Area("Sys")]
    public class PluginController : Controller
    {
        private ISysUserService _userService;

        public PluginController(ISysUserService userService)
        {
            _userService = userService;
        }

        /// <summary>
        /// �û�ѡ����
        /// </summary>
        /// <returns></returns>
        public new async Task<IActionResult> User([FromQuery]UserIndexSearch search)
        {
            if (search.PageIndex.IsDefault())
            {
                search.PageIndex = 1;
            }
            if (search.PageSize.IsDefault())
            {
                search.PageSize = 5;
            }
            var res = await _userService.GetUserPageAsync(search);
            ViewBag.More = search.More;
            return View(res);
        }
    }
}