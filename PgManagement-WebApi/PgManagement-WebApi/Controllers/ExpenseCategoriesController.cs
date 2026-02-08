using Microsoft.AspNetCore.Mvc;
using PgManagement_WebApi.Services;

namespace PgManagement_WebApi.Controllers
{
    [ApiController]
    [Route("api/expense-categories")]
    public class ExpenseCategoriesController : ControllerBase
    {
        private readonly IExpenseCategoryService service;

        public ExpenseCategoriesController(IExpenseCategoryService service)
        {
            this.service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetCategories()
        {
            var categories = await service.GetActiveCategoriesAsync();
            return Ok(categories);
        }
    }

}
