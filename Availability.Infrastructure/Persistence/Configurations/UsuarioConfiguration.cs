using Availability.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Availability.Infrastructure.Persistence.Configurations;

public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        // Tabla donde se almacenarán los usuarios.
        builder.ToTable("Usuarios");

        // Identificador numérico generado por SQLite.
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id)
            .ValueGeneratedOnAdd();

        // Identidad autenticada obligatoria y única.
        builder.Property(u => u.IdentidadExterna)
            .IsRequired()
            .HasMaxLength(256);

        builder.HasIndex(u => u.IdentidadExterna)
            .IsUnique();

        // Correo electrónico del usuario.
        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(256);
    }
}
