using Microsoft.AspNetCore.Mvc;
namespace Transcoder.Controllers;

public class ErrorsController : ControllerBase
{
    [Route("/error")]
    public IActionResult Error()
    {
        return Problem();
    }
}
