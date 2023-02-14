using Microsoft.AspNetCore.Mvc;

namespace Toot2ToulouseWeb
{
    public static class JsonResults
    {
        public static JsonResult Success(object? result=null)
        {
            return new JsonResult(new { Success = true, Result=result });
        }
    }
}
