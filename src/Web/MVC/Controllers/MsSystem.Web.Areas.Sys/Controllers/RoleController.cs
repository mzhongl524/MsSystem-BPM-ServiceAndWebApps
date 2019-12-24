using JadeFramework.Core.Domain.Entities;
using JadeFramework.Core.Domain.Enum;
using JadeFramework.Core.Extensions;
using JadeFramework.Core.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MsSystem.Web.Areas.Sys.Model;
using MsSystem.Web.Areas.Sys.ViewModel;
using MsSystem.Utility;
using MsSystem.Utility.Filters;
using MsSystem.Web.Areas.Sys.Service;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MsSystem.Web.Areas.Sys.Controllers
{
    /// <summary>
    /// ��ɫ����
    /// </summary>
    [Area("Sys")]
    public class RoleController : BaseController
    {
        private ISysRoleService _roleService;
        private ISysResourceService _resourceService;
        public RoleController(ISysRoleService roleService, ISysResourceService resourceService)
        {
            _roleService = roleService;
            _resourceService = resourceService;
        }

        #region ��ɫҳ��

        /// <summary>
        /// ��ɫ�б�
        /// </summary>
        /// <param name="search"></param>
        /// <returns></returns>
        [HttpGet]
        [Permission]
        public async Task<IActionResult> Index(RoleIndexSearch search)
        {
            if (search.PageIndex.IsDefault())
            {
                search.PageIndex = 1;
            }
            if (search.PageSize.IsDefault())
            {
                search.PageSize = 10;
            }
            var res = await _roleService.GetListAsync(search);
            return View(res);
        }

        /// <summary>
        /// ��ɫ�����༭
        /// </summary>
        /// <param name="roleid"></param>
        /// <returns></returns>
        [HttpGet]
        [Permission("/Sys/Role/Index", ButtonType.View)]
        public IActionResult Show(long? roleid)
        {
            ViewBag.TableId = roleid ?? 0;
            return View();
        }

        /// <summary>
        /// ��ɫ�û�����
        /// </summary>
        /// <param name="roleid"></param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Users([FromQuery]long roleid, [FromQuery]int pageIndex = 1, [FromQuery]int pageSize = 10)
        {
            var page = await _roleService.GetRoleUserAsync(roleid, pageIndex, pageSize);
            ViewBag.roleid = roleid;
            return View(page);
        }

        #endregion

        #region Ȩ�޿���

        /// <summary>
        /// ��ȡ�༭��Ϣ
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        [Permission("/Sys/Role/Index", ButtonType.View, false)]
        [ActionName("Get")]
        public async Task<IActionResult> Get([FromQuery]long id)
        {
            var res = await _roleService.GetAsync(id);
            return Ok(res);
        }

        /// <summary>
        /// ����
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Permission("/Sys/Role/Index", ButtonType.Add, false)]
        [ActionName("Add")]
        public async Task<IActionResult> Add([FromBody]SysRole role)
        {
            role.CreateUserId = UserIdentity.UserId;
            bool res = await _roleService.AddAsync(role);
            return Ok(res);
        }

        /// <summary>
        /// �༭
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Permission("/Sys/Role/Index", ButtonType.Edit, false)]
        [ActionName("Update")]
        public async Task<IActionResult> Update([FromBody]SysRole role)
        {
            role.UpdateUserId = UserIdentity.UserId;
            bool res = await _roleService.UpdateAsync(role);
            return Ok(res);
        }

        /// <summary>
        /// �߼�ɾ��
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        [HttpPost]
        [Permission("/Sys/Role/Index", ButtonType.Delete, false)]
        [ActionName("Delete")]
        public async Task<IActionResult> Delete([FromBody]List<long> ids)
        {
            long userid = UserIdentity.UserId;
            var res = await _roleService.DeleteAsync(ids, userid);
            return Ok(res);
        }

        #endregion

        #region ��ɫ������Դ

        /// <summary>
        /// ��ɫ������Դ
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Authorize]
        [ActionName("Box")]
        public async Task<IActionResult> Box([FromQuery]long roleid)
        {
            if (roleid <= 0)
            {
                return NotFound();
            }
            var res = await _resourceService.GetBoxTreeAsync(roleid);
            return Ok(res);
        }

        /// <summary>
        /// ��ɫ������Դ����
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        [HttpPost]
        [Authorize]
        [ActionName("BoxSave")]
        public async Task<IActionResult> BoxSave([FromBody]RoleTreeDto dto)
        {
            if (dto.RoleId <= 0)
            {
                return NotFound();
            }

            dto.CreateUserId = UserIdentity.UserId;
            var res = await _resourceService.BoxSaveAsync(dto);
            return Ok(res);
        }
        #endregion

        #region ��ɫ�����û�

        /// <summary>
        /// ɾ����ɫ�µ��û�
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        [HttpPost]
        [Authorize]
        [ActionName("DeleteUser")]
        public async Task<IActionResult> DeleteUser([FromBody]RoleToUserDto dto)
        {
            dto.CurrentUserId = UserIdentity.UserId;
            bool res = await _roleService.DeleteUserAsync(dto);
            return Ok(res);
        }

        /// <summary>
        /// ��ӽ�ɫ�µ��û�
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        [HttpPost]
        [Authorize]
        [ActionName("AddUser")]
        public async Task<IActionResult> AddUser([FromBody]RoleToUserDto dto)
        {
            dto.CurrentUserId = UserIdentity.UserId;
            bool res = await _roleService.AddUserAsync(dto);
            return Ok(res);
        }

        #endregion

    }
}