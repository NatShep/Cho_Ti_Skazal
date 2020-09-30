using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Chotiskazal.App;
using Chotiskazal.DAL;
using Chotiskazal.Dal.Enums;
using Chotiskazal.Dal.Services;
using Chotiskazal.LogicR.yapi;
using static Chotiskazal.LogicR.yapi.MapperForDBModels;
using Chotiskazal.WebApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Chotiskazal.WebApp.Controllers
{
    public class ExamController : Controller
    {
        private readonly UsersWordService _usersWordService;
        private readonly UserService _userService;
        private readonly DictionaryService _dictionaryService;

        public ExamController(DictionaryService dictionaryService, UsersWordService usersWordService,
            UserService userService)
        {
            _dictionaryService = dictionaryService;
            _usersWordService = usersWordService;
            _userService = userService;
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> ExamMenu() => View();
    }
}