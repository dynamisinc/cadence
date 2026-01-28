using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Cadence.WebApi.temp;

public partial class CadenceContext : DbContext
{
    public CadenceContext()
    {
    }

    public CadenceContext(DbContextOptions<CadenceContext> options)
        : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlServer("Name=ConnectionStrings:DefaultConnection");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
