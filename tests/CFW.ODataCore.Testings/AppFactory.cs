

namespace CFW.ODataCore.Testings;

public class AppFactory : WebApplicationFactory<Program>, IDisposable
{
    public AppFactory()
    {
        //Delete all sqlite databases
        var dbContextPath = "appdbcontext*.db*";
        var files = Directory.GetFiles(Directory.GetCurrentDirectory(), dbContextPath);
        foreach (var file in files)
            File.Delete(file);
    }
}
