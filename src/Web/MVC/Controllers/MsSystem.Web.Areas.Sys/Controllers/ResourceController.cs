using JadeFramework.Core.Domain.Enum;
using JadeFramework.Core.Extensions;
using JadeFramework.Core.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MsSystem.Web.Areas.Sys.ViewModel;
using MsSystem.Utility;
using MsSystem.Utility.Filters;
using MsSystem.Web.Areas.Sys.Service;
using System.Threading.Tasks;

namespace MsSystem.Web.Areas.Sys.Controllers
{
    /// <summary>
    /// ��Դ
    /// </summary>
    [Area("Sys")]
    public class ResourceController : BaseController
    {
        private ISysResourceService _resourceService;
        public ResourceController(ISysResourceService resourceService)
        {
            _resourceService = resourceService;
        }

        #region ҳ��
        /// <summary>
        /// �˵��б�
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Permission]
        public async Task<IActionResult> Index([FromQuery]long systemid)
        {
            var res = await _resourceService.GetTreeAsync(systemid);
            return View(res);
        }
        /// <summary>
        /// SHOW
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Permission("/Sys/Resource/Index", ButtonType.View)]
        public IActionResult Show()
        {
            return View();
        }
        /// <summary>
        /// ICONͼ��
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Authorize]
        public IActionResult Icon()
        {
            return View();
        }
        #endregion

        #region Ȩ�޿���

        /// <summary>
        /// �༭���ݻ�ȡ
        /// </summary>
        /// <param name="id">�˵�ID</param>
        /// <param name="systemid">ϵͳID</param>
        /// <returns></returns>
        [HttpGet]
        [Permission("/Sys/Resource/Index", ButtonType.View, false)]
        [ActionName("Get")]
        public async Task<IActionResult> Get([FromQuery]long id, [FromQuery]long systemid)
        {
            ResourceShowViewModel domain = await _resourceService.GetResourceAsync(id, systemid);
            return Ok(domain);
        }

        /// <summary>
        /// ����
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Permission("/Sys/Resource/Index", ButtonType.Add, false)]
        [ActionName("Add")]
        public async Task<IActionResult> Add([FromBody]ResourceShowDto dto)
        {
            dto.SysResource.CreateUserId = UserIdentity.UserId;
            bool res = await _resourceService.AddAsync(dto);
            return Ok(res);
        }

        /// <summary>
        /// �༭
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Permission("/Sys/Resource/Index", ButtonType.Edit, false)]
        [ActionName("Edit")]
        public async Task<IActionResult> Edit([FromBody]ResourceShowDto dto)
        {
            dto.SysResource.CreateUserId = UserIdentity.UserId;
            bool res = await _resourceService.UpdateAsync(dto);
            return Ok(res);
        }

        /// <summary>
        /// ɾ��
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        [HttpPost]
        [Permission("/Sys/Resource/Index", ButtonType.Delete, false)]
        [ActionName("Delete")]
        public async Task<IActionResult> Delete([FromBody]long[] ids)
        {
            long userid = UserIdentity.UserId;
            var res = await _resourceService.DeleteAsync(ids, userid);
            return Ok(res);
        }

        #endregion

    }
}