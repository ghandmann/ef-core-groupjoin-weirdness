using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace GroupJoinTest {
    namespace Models {
        public class User {
            public int Id { get; set; }
            public string Name { get; set; }
            public List<UserRoles> UserRoles { get; set; }
        }

        public class Role {
            public int Id { get; set; }
            public string Name { get; set; }
            public List<UserRoles> UserRoles { get; set; }
        }

        public class UserRoles {
            public int UserId { get; set; }
            public User User { get; set; }
            public int RoleId { get; set; }
            public Role Role { get; set; }
        }

        public class DemoContext : DbContext {
            public DemoContext (DbContextOptions<DemoContext> options) : base (options) { }
            public DbSet<User> Users { get; set; }
            public DbSet<Role> Roles { get; set; }
            public DbSet<UserRoles> UserRoles { get; set; }

            protected override void OnModelCreating (ModelBuilder modelBuilder) {
                //base.OnModelCreating (modelBuilder);

                // Define a composite primary key in UserRole
                modelBuilder.Entity<UserRoles> ().HasKey (ur => new { ur.UserId, ur.RoleId });
                // Define the 1:n relation between UserRoles and User
                modelBuilder.Entity<UserRoles> ().HasOne (ur => ur.User).WithMany (u => u.UserRoles).HasForeignKey (ur => ur.UserId);
                // Define the 1:n relation between UserRoles and Role
                modelBuilder.Entity<UserRoles> ().HasOne (ur => ur.Role).WithMany (r => r.UserRoles).HasForeignKey (ur => ur.RoleId);
            }

        }
    }

}