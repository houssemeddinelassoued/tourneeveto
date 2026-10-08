using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using TourneeVeto.Ui;
using TourneeVeto.Web;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Services communs à tous les hôtes (stockage IndexedDB, jeu de démonstration), définis dans la RCL.
builder.Services.AddTourneeVeto();

await builder.Build().RunAsync();
