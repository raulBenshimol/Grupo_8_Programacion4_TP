using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Availability.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Availability.Infrastructure.Persistence.Configurations;

public class AsientoConfiguration : IEntityTypeConfiguration<Asiento>
{
    public void Configure(EntityTypeBuilder<Asiento> builder)
    {
        builder.ToTable("Asientos");
        builder.HasKey(a => a.Id);

        // El código del asiento es obligatorio, máximo 20 caracteres.
        builder.Property(a => a.Codigo)
            .IsRequired()
            .HasMaxLength(20);

        // Guardamos el Enum como texto en SQLite.
        builder.Property(a => a.Estado)
            .HasConversion<string>()
            .HasMaxLength(20);

        // Identifica la solicitud asociada a una retención.
        // Es opcional porque los asientos existentes no tienen esta relación.
        builder.Property(a => a.SolicitudReservaId)
            .IsRequired(false);
    }
}
