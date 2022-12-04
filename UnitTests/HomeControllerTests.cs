using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToDoList.Controllers;
using ToDoList.Models;
using ToDoList.Models.Repo;
using ToDoList.Models.ViewModels;
using Xunit;

namespace ToDoList.Tests.UnitTests
{
    public class HomeControllerTests
    {
        private readonly HomeController _controller;

        public HomeControllerTests()
        {
            _controller = GetHomeController();
        }

        [Fact]
        public void Index_Returns_ViewResult_And_Check_ViewModel()
        {
            // Act
            var result = _controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.IsAssignableFrom<TaskListViewModel>(
                viewResult.ViewData.Model);

        }

        [Theory]
        [MemberData(nameof(TestModelData_For_Index))]
        public void Index_Check_UserTasks(TaskListViewModel model)
        {
            // Assert
            Assert.Collection(model.Tasks.AsEnumerable().OrderBy(t=>t.MakingDate),
                t => Assert.Equal("Make Aplication", t.Title),
                t => Assert.Equal("Make Testing", t.Title),
                t => Assert.Equal("Go to the Gym", t.Title),
                t => Assert.Equal("Go to the Shop", t.Title),
                t => Assert.Equal("Buy Laptop", t.Title),
                t => Assert.Equal("Repair Smart Watch", t.Title)
                );
        }

        [Theory]
        [MemberData(nameof(TestModelData_For_Index))]
        public void Index_CheckTaskCategories(TaskListViewModel model)
        {
            // Assert
            Assert.Collection<TaskCategory>(model.Categories.AsEnumerable<TaskCategory>(),
                c => Assert.Equal("My Day", c.Category),
                c => Assert.Equal("Important", c.Category),
                c => Assert.Equal("Planned", c.Category)
                );
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void TaskByCategory_Returns_View_And_Check_ViewModel(int id)
        {
            // Act
            var result = _controller.TaskByCategory(id);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.IsAssignableFrom<TaskListViewModel>(
                viewResult.ViewData.Model);
        }

        [Fact]
        public void TaskByCategory_Returns_View_And_Check_ViewModel_IncorrectId()
        {
            // Arrange
            const int INCORRECT_ID = 4;

            // Assert
            Assert.Throws<NullReferenceException>(() => _controller.TaskByCategory(INCORRECT_ID));
        }

        [Fact]
        public void TaskByCategory_Returns_View_And_Check_ViewModel_IncorrectId_Zero()
        {
            // Arrange
            const int INCORRECT_ID = 0;
            string expected = "/";

            // Act
            var result = _controller.TaskByCategory(INCORRECT_ID);

            //Assert
            var viewResult = Assert.IsType<RedirectResult>(result);
            Assert.Equal(expected, viewResult.Url);
        }

        [Fact]
        public void Check_Sidebar_menu()
        {
            // Arrange
            var expected = GetTestItemsSideBar();

            // Act
            var result = GetHomeController().Index();
            var viewResult = Assert.IsType<ViewResult>(result);

            //Assert
            IEnumerable<ItemSideBar> actual = (IEnumerable<ItemSideBar>)viewResult.ViewData["SideBarItems"];
            Assert.Collection<ItemSideBar>(actual,
                item => Assert.Equal(expected.FirstOrDefault(p => p.Id == 1).Name, item.Name),
                item => Assert.Equal(expected.FirstOrDefault(p => p.Id == 2).Name, item.Name),
                item => Assert.Equal(expected.FirstOrDefault(p => p.Id == 3).Name, item.Name),
                item => Assert.Equal(expected.FirstOrDefault(p => p.Id == 0).Name, item.Name)
                );
        }

        [Fact]
        public void AddUserTaskReturnsARedirectAndAddsUserValidInput()
        {
            // Arrange
            var mock = new Mock<ITaskRepository>();
            var mockRepoToDoMenu = new Mock<IToDoMenu>();
            var userTask = new UserTask() { Title = "Go to the shop", MakingDate = DateTime.Now, IsDone = false, Dedline = DateTime.Now };
            var controller = new HomeController(null, mockRepoToDoMenu.Object, mock.Object);
            //Act
            var result = controller.Create(userTask, 1, "My Day");

            //Assert
            var redirectToActionResult = Assert.IsType<RedirectResult>(result.Result);
            Assert.Equal("TaskByCategory/1", redirectToActionResult.Url);
            mock.Verify(mock => mock.AddUserTask(userTask), Times.Once());
        }

        [Fact]
        public void AddUserTaskReturnsARedirectAndAddsUserInValidInput()
        {
            // Arrange
            var mock = new Mock<ITaskRepository>();
            var mockRepoToDoMenu = new Mock<IToDoMenu>();
            var userTask = new UserTask() { Title = "G", MakingDate = DateTime.Now, IsDone = false, Dedline = DateTime.Now };
            var controller = new HomeController(null, mockRepoToDoMenu.Object, mock.Object);
            controller.ModelState.AddModelError("Title", "ASDsc");
            //Act
            var result = controller.Create(userTask, 1, "My Day");

            //Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.IsType<SerializableError>(badRequestResult.Value);
        }

        [Fact]
        public void DeleteUserTaskReturnsARedirectValidInput()
        {
            // Arrange
            var mock = new Mock<ITaskRepository>();
            var mockRepoToDoMenu = new Mock<IToDoMenu>();
            var controller = new HomeController(null,  mockRepoToDoMenu.Object, mock.Object);
            //Act
            var result = controller.Delete(1, 1);

            //Assert
            var redirectToActionResult = Assert.IsType<RedirectResult>(result.Result);
            Assert.Equal("/Home/TaskByCategory/1", redirectToActionResult.Url);
            mock.Verify(mock => mock.DeleteUserTask(1), Times.Once());
        }

        [Fact]
        public void DeleteUserTaskReturnsARedirectInValidInput()
        {
            // Arrange
            var mock = new Mock<ITaskRepository>();
            var mockRepoToDoMenu = new Mock<IToDoMenu>();
            var controller = new HomeController(null,  mockRepoToDoMenu.Object, mock.Object);
            //Act
            var result = controller.Delete(null, 1);

            //Assert
            var redirectNotFoundResult = Assert.IsType<NotFoundResult>(result.Result);
            Assert.Equal(StatusCodes.Status404NotFound, redirectNotFoundResult.StatusCode);
            mock.Verify(mock => mock.DeleteUserTask(1), Times.Never);
        }

        [Theory]
        [InlineData(1, 1, 1)]
        [InlineData(2, 2, 2)]
        [InlineData(3, 3, 3)]
        [InlineData(4, 1, 4)]
        [InlineData(5, 2, 5)]
        [InlineData(6, 3, 6)]
        //[InlineData(7, 0)]
        public void UpdateUserTaskStatusReturnsARedirectValidInput(int id, int routeId, int expextedId)
        {
            // Arrange
            var mock = new Mock<ITaskRepository>();
            mock.Setup(repo => repo.Tasks).Returns(GetTestUserTask());
            mock.Setup(repo => repo.ChangeStatusUserTask(It.IsAny<int>())).Returns(Task.FromResult(expextedId));
            var mockRepoToDoMenu = new Mock<IToDoMenu>();
            mock.Setup(repo => repo.Tasks).Returns(GetTestUserTask());

            var controller = new HomeController(null, mockRepoToDoMenu.Object, mock.Object);
            //Act
            var result = controller.ChangeTaskStatus(id, routeId);
            string expectedRoute = "/Home/TaskByCategory/" + routeId;

            //Assert
            var redirectToActionResult = Assert.IsType<RedirectResult>(result.Result);
            Assert.Equal(expectedRoute, redirectToActionResult.Url);
            Assert.Equal(expextedId, id);

        }

        [Fact]
        public void UpdateUserTaskStatusReturnsARedirectInValidInput()
        {
            // Arrange
            int routeId = 2;
            int inputId = 7;
            int expetedId = 0;
            var mock = new Mock<ITaskRepository>();
            mock.Setup(repo => repo.Tasks).Returns(GetTestUserTask());
            mock.Setup(repo => repo.ChangeStatusUserTask(It.IsAny<int>())).Returns(Task.FromResult(expetedId));
            var mockRepoToDoMenu = new Mock<IToDoMenu>();
            mock.Setup(repo => repo.Tasks).Returns(GetTestUserTask());
            var controller = new HomeController(null, mockRepoToDoMenu.Object, mock.Object);
            //Act
            var result = controller.ChangeTaskStatus(inputId, routeId);

            //Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.IsType<SerializableError>(badRequestResult.Value);

        }
        [Fact]
        public void UpdateUserTaskStatusReturnsARedirectInputNull()
        {
            // Arrange
            int routeId = 2;
            int expetedId = 0;
            var mock = new Mock<ITaskRepository>();
            mock.Setup(repo => repo.Tasks).Returns(GetTestUserTask());
            mock.Setup(repo => repo.ChangeStatusUserTask(It.IsAny<int>())).Returns(Task.FromResult(expetedId));
            var mockRepoToDoMenu = new Mock<IToDoMenu>();
            mock.Setup(repo => repo.Tasks).Returns(GetTestUserTask());
            var controller = new HomeController(null, mockRepoToDoMenu.Object, mock.Object);

            //Act
            var result = controller.ChangeTaskStatus(null, routeId);

            //Assert
            var notFoundResult = Assert.IsType<NotFoundResult>(result.Result);
            Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
        }

        [Theory]
        [InlineData(1, 2)]
        [InlineData(2, 2)]
        [InlineData(3, 1)]
        [InlineData(4, 1)]
        [InlineData(5, 3)]
        [InlineData(6, 3)]
        public void EditUserTaskStatusReturnsARedirectValidInput(int id, int expextedRouteId)
        {
            // Arrange
            var mock = new Mock<ITaskRepository>();
            mock.Setup(repo => repo.Tasks).Returns(GetTestUserTask());
            mock.Setup(repo => repo.GetUserTask(It.IsAny<int>())).Returns(Task<UserTask>.FromResult(mock.Object.Tasks.FirstOrDefault(t => t.Id == id)));
            mock.Object.Tasks.FirstOrDefault(t => t.Id == id);
            new Task<UserTask>(() => new UserTask());
            var mockRepoToDoMenu = new Mock<IToDoMenu>();
            mockRepoToDoMenu.Setup(repo => repo.itemsSideBars).Returns(GetTestItemsSideBar());
            
            var controller = new HomeController(null, mockRepoToDoMenu.Object, mock.Object);
            //Act
            var result = controller.EditUserTask(id);

            //Assert
            var viewToActionResult = Assert.IsType<ViewResult>(result.Result);
            Assert.IsAssignableFrom<TaskListViewModel>(
                viewToActionResult.ViewData.Model);
            Assert.Equal(expextedRouteId, viewToActionResult.ViewData["CurrentRoute"]);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(7)]
        public void EditUserTaskStatusReturnsARedirectInvalidInput(int id)
        {
            // Arrange
            var mock = new Mock<ITaskRepository>();
            mock.Setup(repo => repo.Tasks).Returns(GetTestUserTask());
            mock.Setup(repo => repo.GetUserTask(It.IsAny<int>())).Returns(Task<UserTask>.FromResult(mock.Object.Tasks.FirstOrDefault(t => t.Id == id)));
            mock.Object.Tasks.FirstOrDefault(t => t.Id == id);
            new Task<UserTask>(() => new UserTask());
            var mockRepoToDoMenu = new Mock<IToDoMenu>();
            mock.Setup(repo => repo.Tasks).Returns(GetTestUserTask());
            var controller = new HomeController(null, mockRepoToDoMenu.Object, mock.Object);

            //Act
            var result = controller.EditUserTask(id);

            //Assert
            var viewToBadRequestObjectReesult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.IsType<SerializableError>(viewToBadRequestObjectReesult.Value);
            Assert.Equal(StatusCodes.Status400BadRequest, viewToBadRequestObjectReesult.StatusCode);
        }
        [Fact]
        public void EditUserTaskStatusReturnsARedirectInputNullId()
        {
            // Arrange
            int id = 0;
            var mock = new Mock<ITaskRepository>();
            mock.Setup(repo => repo.Tasks).Returns(GetTestUserTask());
            mock.Setup(repo => repo.GetUserTask(It.IsAny<int>())).Returns(Task<UserTask>.FromResult(mock.Object.Tasks.FirstOrDefault(t => t.Id == id)));
            mock.Object.Tasks.FirstOrDefault(t => t.Id == id);
            new Task<UserTask>(() => new UserTask());
            var mockRepoToDoMenu = new Mock<IToDoMenu>();
            mock.Setup(repo => repo.Tasks).Returns(GetTestUserTask());
            var controller = new HomeController(null, mockRepoToDoMenu.Object, mock.Object);

            //Act
            var result = controller.EditUserTask(null);

            //Assert
            var notFoundReesult = Assert.IsType<NotFoundResult>(result.Result);
            Assert.Equal(StatusCodes.Status404NotFound, notFoundReesult.StatusCode);
        }

        [Theory]
        [InlineData(1, 2)]
        [InlineData(2, 2)]
        [InlineData(3, 1)]
        [InlineData(4, 1)]
        [InlineData(5, 3)]
        [InlineData(6, 3)]
        public void EditUserTaskConfirmStatusReturnsARedirectValidInput(int id, int expetedRouteId)
        {
            // Arrange
            const string FIRST_PART_OF_ROUTE = "/Home/TaskByCategory/";
            var mock = new Mock<ITaskRepository>();
            mock.Setup(repo => repo.Tasks).Returns(GetTestUserTask());
            mock.Setup(repo => repo.EditUserTask(It.IsAny<UserTask>())).Returns(Task.FromResult(id));
            mock.Object.Tasks.FirstOrDefault(t => t.Id == id);
            new Task<UserTask>(() => new UserTask());
            var mockRepoToDoMenu = new Mock<IToDoMenu>();
            mockRepoToDoMenu.Setup(repo => repo.itemsSideBars).Returns(GetTestItemsSideBar());
            var controller = new HomeController(null, mockRepoToDoMenu.Object, mock.Object);
            string expectRoute = FIRST_PART_OF_ROUTE + expetedRouteId;

            //Act
            var result = controller.EditUserTaskConfirm(mock.Object.Tasks.FirstOrDefault(t => t.Id == id));
            
            //Assert
            var redirectResult = Assert.IsType<RedirectResult>(result.Result);
            Assert.Equal(expectRoute, redirectResult.Url);
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(7, 0)]
        public void EditUserTaskConfirmStatusReturnsANotFoundTask(int id, int expectedId)
        {
            // Arrange moq
            var mock = new Mock<ITaskRepository>();
            mock.Setup(repo => repo.Tasks).Returns(GetTestUserTask());
            mock.Setup(repo => repo.EditUserTask(It.IsAny<UserTask>())).Returns(Task.FromResult(expectedId));
            var mockRepoToDoMenu = new Mock<IToDoMenu>();
            mockRepoToDoMenu.Setup(repo => repo.itemsSideBars).Returns(GetTestItemsSideBar());

            // Arrange
            var userTask = new UserTask() {Id=id, Title="Test Task", Dedline=DateTime.Now.AddDays(1), IsDone = false, MakingDate = DateTime.Now, TaskCategory="My Day" };
            var controller = new HomeController(null, mockRepoToDoMenu.Object, mock.Object);

            //Act
            var result = controller.EditUserTaskConfirm(userTask);

            //Assert
            var notFoundResult = Assert.IsType<NotFoundResult>(result.Result);
            Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
        }

        [Fact]
        public void EditUserTaskConfirmStatusReturnsABadRequest()
        {
            // Arrange moq
            var mock = new Mock<ITaskRepository>();
            mock.Setup(repo => repo.Tasks).Returns(GetTestUserTask());
            var mockRepoToDoMenu = new Mock<IToDoMenu>();
            mockRepoToDoMenu.Setup(repo => repo.itemsSideBars).Returns(GetTestItemsSideBar());

            // Arrange
            var userTask = new UserTask() { Id = 1, Title = "T", Dedline = DateTime.Now.AddDays(1), IsDone = false, MakingDate = DateTime.Now, TaskCategory = "My Day" };
            var controller = new HomeController(null, mockRepoToDoMenu.Object, mock.Object);
            controller.ModelState.AddModelError("Title", "Incorrect lenght");

            //Act
            var result = controller.EditUserTaskConfirm(userTask);

            //Assert
            var badReqeustObjectResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status400BadRequest, badReqeustObjectResult.StatusCode);
        }

        [Fact]
        public void Privacy_Returns_ViewResultl()
        {
            // Arrange
            const string EXPECTED_NAME = "My title Privacy";

            //Act
            var result = _controller.Privacy();

            //Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(EXPECTED_NAME, viewResult.ViewData["Title"]);
        }

        //Testing Data

        public static IEnumerable<object[]> TestModelData_For_Index()
        {
            var controller = GetHomeController();
            var result = controller.Index();
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<TaskListViewModel>(
                viewResult.ViewData.Model);
            return new[] { new object[] { model } };
        }

        private static HomeController GetHomeController()
        {
            var mockRepoUserTask = new Mock<ITaskRepository>();
            mockRepoUserTask.Setup(repo => repo.Tasks).Returns(GetTestUserTask());
            mockRepoUserTask.Setup(repo => repo.Categories).Returns(GetTaskCategories());
            var mockRepoToDoMenu = new Mock<IToDoMenu>();
            mockRepoToDoMenu.Setup(repo => repo.itemsSideBars).Returns(GetTestItemsSideBar());
            return new HomeController(null, mockRepoToDoMenu.Object, mockRepoUserTask.Object);
        }

        private static IQueryable<UserTask> GetTestUserTask()
        {
            var userTasks = new List<UserTask>();
            userTasks.Add(new UserTask() { Id = 1, Title = "Make Aplication", TaskCategory = "Important", MakingDate = DateTime.Now, Dedline = DateTime.Now });
            userTasks.Add(new UserTask() { Id = 2, Title = "Make Testing", TaskCategory = "Important", MakingDate = DateTime.Now, Dedline = DateTime.Now });
            userTasks.Add(new UserTask() { Id = 3, Title = "Go to the Gym", TaskCategory = "My Day", MakingDate = DateTime.Now, Dedline = DateTime.Now });
            userTasks.Add(new UserTask() { Id = 4, Title = "Go to the Shop", TaskCategory = "My Day", MakingDate = DateTime.Now, Dedline = DateTime.Now });
            userTasks.Add(new UserTask() { Id = 5, Title = "Buy Laptop", TaskCategory = "Planned", MakingDate = DateTime.Now, Dedline = DateTime.Now });
            userTasks.Add(new UserTask() { Id = 6, Title = "Repair Smart Watch", TaskCategory = "Planned", MakingDate = DateTime.Now, Dedline = DateTime.Now });
            return userTasks.AsQueryable();
        }

        private static IEnumerable<ItemSideBar> GetTestItemsSideBar()
        {
            var itemsSideBar = new List<ItemSideBar>();
            itemsSideBar.Add(new ItemSideBar() { Id = 1, Name = "My Day", IsActiv = false });
            itemsSideBar.Add(new ItemSideBar() { Id = 2, Name = "Important", IsActiv = false });
            itemsSideBar.Add(new ItemSideBar() { Id = 3, Name = "Planned", IsActiv = false });
            itemsSideBar.Add(new ItemSideBar() { Id = 0, Name = "All", IsActiv = false });
            return itemsSideBar;
        }

        private static IQueryable<TaskCategory> GetTaskCategories()
        {
            var taskCategories = new List<TaskCategory>();
            taskCategories.Add(new TaskCategory() { Id = 1, Category = "My Day", IsDeleted = false });
            taskCategories.Add(new TaskCategory() { Id = 2, Category = "Important", IsDeleted = false });
            taskCategories.Add(new TaskCategory() { Id = 3, Category = "Planned", IsDeleted = false });
            return taskCategories.AsQueryable();
        }
    }
}
