using AgileManagement.Application;
using AgileManagement.Domain;
using AgileManagement.Domain.models;
using AgileManagement.Mvc.Areas.Admin.Models;
using AgileManagement.Mvc.Controllers;
using AgileManagement.Mvc.Services;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AgileManagement.Mvc.Areas.Admin.Controllers
{
    [Area("Admin")]
    // controller areas içerinde ise area olarak işaretleriz
    public class ProjectController : AuthBaseController
    {
        private readonly IProjectRepository _projectRepository;
        private readonly IUserRepository _userRepository;
        private readonly IProjectWithContributorsRequestService _projectWithContributorsRequestService;
        private readonly IMapper _mapper;
        private readonly IContributorProjectAccessApprovementService _contributorProjectAccessApprovementService;
        private readonly ISprintService _sprintService;
        private readonly ISprintAddService _sprintAddService;


        public ProjectController(IProjectRepository projectRepository, IUserRepository userRepository, IProjectWithContributorsRequestService projectWithContributorsRequestService, IMapper mapper, AuthenticatedUser authenticatedUser, IContributorProjectAccessApprovementService contributorProjectAccessApprovementService, ISprintService sprintService, ISprintAddService sprintAddService) : base(authenticatedUser)
        {
            _projectRepository = projectRepository;
            _userRepository = userRepository;
            _projectWithContributorsRequestService = projectWithContributorsRequestService;
            _mapper = mapper;
            _contributorProjectAccessApprovementService = contributorProjectAccessApprovementService;
            _sprintService = sprintService;
            _sprintAddService = sprintAddService;
        }

        public IActionResult Index()
        {

            return View();
        }



        public IActionResult Management()
        {

            //var userId = User.Claims.First(x => x.Type == "UserId").Value;

            var request = new ProjectWithContributorRequestDto
            {
                CreatedBy = authUser.UserId,
                ProjectId = null
            };

            var response = _projectWithContributorsRequestService.OnProcess(request);

            return View(response.Projects);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Create(ProjectCreateInputModel projectCreateInputModel)
        {
            if (ModelState.IsValid)
            {

                var project = new Project(name: projectCreateInputModel.Name, description: projectCreateInputModel.Description, authUser.UserId);

                _projectRepository.Add(project);
                _projectRepository.Save();

                ViewBag.Message = "Proje oluşturuldu";

                return View();
            }

            // Proje oluşturma sayfası
            return View();
        }


        [HttpGet]
        public IActionResult AddContributorRequest(string projectId)
        {
            var response = _projectWithContributorsRequestService.OnProcess(new ProjectWithContributorRequestDto { ProjectId = projectId });

            var projectContributorsId = response.Projects[0].Contributors.Select(x => x.UserId).ToList();

            // projeye tanımlanmış olan userların dropdownı
            ViewBag.UsersWithNoContributors = _userRepository.GetQuery().Where(x => projectContributorsId.Contains(x.Id) == false).Select(a => new SelectListItem
            {
                Text = a.Email,
                Value = a.Id.ToString()
            });


            return View(response.Projects[0]);

        }



        [HttpPost]
        public JsonResult AddContributorRequest([FromBody] ContributorInputModel model)
        {
            var project = _projectRepository.Find(model.ProjectId);

            foreach (var userId in model.UsersId)
            {
                project.AddContributor(new Contributor(userId));
            }

            _projectRepository.Save();


            return Json("OK");

        }

        [HttpGet]
        public IActionResult AddSprintRequest(string projectId)
            {
            var response = _sprintService.OnProcess(projectId);
            return View(response);
        }



        [HttpPost]
        public JsonResult AddSprintRequest([FromBody] SprintInputModel model)
        {

            var dto = _mapper.Map<SprintInputModel, AddSprintRequestDto>(model);
            var response = _sprintAddService.OnProcess(dto);
            return Json(response);
        }
    }
}
