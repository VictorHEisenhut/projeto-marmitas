using Marmitas.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Marmitas.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20261006160000_CreateRestaurantsAndOrders")]
    partial class CreateRestaurantsAndOrders
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
        }
    }
}

