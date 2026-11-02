using Marmitas.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Marmitas.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20261102191000_CreatePlatformModules")]
    partial class CreatePlatformModules
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
        }
    }
}

