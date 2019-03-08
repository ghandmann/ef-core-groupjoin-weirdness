using System.Collections.Generic;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Xunit;

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

    namespace Test {
        using System.Linq;
        using System;
        using global::GroupJoinTest.Models;
        using Microsoft.Data.Sqlite;
        using Microsoft.Extensions.Logging;
        using Microsoft.Extensions.Logging.Console;

        public class GroupJoinTest {
            public static readonly LoggerFactory MyLoggerFactory
                = new LoggerFactory(new[] {new ConsoleLoggerProvider((_, __) => true, true)});
            public void InsertUsers(DemoContext context) {
                var users = new List<User> {
                    new User () { Id = 1, Name = "User 1" },
                    new User () { Id = 2, Name = "User 2" },
                };
                context.Users.AddRange (users);
            }

            public void InsertRoles(DemoContext context) {
                var roles = new List<Role> {
                    new Role () { Id = 1, Name = "Role 1" },
                    new Role () { Id = 2, Name = "Role 2" },
                };
                context.Roles.AddRange (roles);
            }



            public void AssertEmptyContext(DemoContext context) {
                Assert.Empty(context.Users);
                Assert.Empty(context.Roles);
                Assert.Empty(context.UserRoles);
            }

            public void AssertEmptyUserRolesLinkTable(DemoContext context, UserRoles userRole) {
                var beforeResult = context.UserRoles.Find(userRole.UserId, userRole.RoleId);
                Assert.Null(beforeResult);
            }

            public void AssertHasMatchingUserRoleLink(DemoContext context, UserRoles userRole) {
                var beforeResult = context.UserRoles.Find(userRole.UserId, userRole.RoleId);
                Assert.NotNull(beforeResult);
                Assert.Equal(userRole.UserId, beforeResult.UserId);
                Assert.Equal(userRole.RoleId, beforeResult.RoleId);
            }

            [Fact]
            public void UserWithoutRoles () {
                var context = GetInMemoryContext();
                InsertUsers(context);
                InsertRoles(context);
                context.SaveChanges ();

                var result = GetRolesByUser (context, 1);

                // We expect that all roles are returned
                Assert.Equal (2, result.Count);

                foreach (var role in result) {
                    // The navigational property must be set
                    Assert.NotNull (role.UserRoles);
                    // But should not contain any results
                    Assert.Empty (role.UserRoles);
                }
            }

            [Fact]
            public void UserWithOneRole () {
                var linkedUserRole = new UserRoles () { UserId = 1, RoleId = 1 };

                using(var writeContext = GetInMemoryContext(nameof(UserWithOneRole))) {
                    InsertUsers(writeContext);
                    InsertRoles(writeContext);

                    writeContext.UserRoles.Add (linkedUserRole);

                    writeContext.SaveChanges ();
                }

                using(var readContext = GetInMemoryContext(nameof(UserWithOneRole))) {
                    var result = GetRolesByUser (readContext, 1);

                    // In total we expect 2 roles
                    Assert.Equal (2, result.Count);

                    foreach (var role in result) {
                        // The navigational property must be set
                        Assert.NotNull (role.UserRoles);

                        if (role.Id == linkedUserRole.RoleId) {
                            // For the role with ID 1 we should get back one user (UserId=1)
                            Assert.Single (role.UserRoles);
                            Assert.Equal(linkedUserRole.UserId, role.UserRoles.First().UserId);
                        } else {
                            // All other role result must not have any UserRoles entries
                            Assert.Empty (role.UserRoles);
                        }
                    }
                }
            }



            [Fact]
            public void UserWithAllRoles () {
                using(var writeContext = GetInMemoryContext(nameof(UserWithAllRoles))) {
                    InsertUsers(writeContext);
                    InsertRoles(writeContext);

                    writeContext.UserRoles.Add (new UserRoles () { UserId = 1, RoleId = 1 });
                    writeContext.UserRoles.Add (new UserRoles () { UserId = 1, RoleId = 2 });

                    writeContext.SaveChanges ();
                }

                using(var readContext = GetInMemoryContext(nameof(UserWithAllRoles))) {
                    var result = GetRolesByUser (readContext, 1);

                    // In total we expect 4 roles
                    Assert.Equal (2, result.Count);

                    foreach (var role in result) {
                        // The navigational property must be set
                        Assert.NotNull (role.UserRoles);
                        // And should return one result
                        Assert.Single (role.UserRoles);
                    }
                }
            }

            [Fact]
            public void TwoUserWithDifferentRoles_SQLite () {
                var sqlite = new SqliteConnection("DataSource=:memory:");
                sqlite.Open();

                using(var writeContext = GetInMemorySqliteContext(sqlite)) {

                    InsertUsers(writeContext);
                    InsertRoles(writeContext);

                    writeContext.UserRoles.Add (new UserRoles () { UserId = 1, RoleId = 1 });
                    writeContext.UserRoles.Add (new UserRoles () { UserId = 2, RoleId = 2 });

                    writeContext.SaveChanges ();
                }

                using(var readContext = GetInMemorySqliteContext(sqlite)) {
                    var result = GetRolesByUser (readContext, 1);

                    // In total we expect 4 roles
                    Assert.Equal (2, result.Count);

                    foreach (var role in result) {
                        // The navigational property must be set
                        Assert.NotNull (role.UserRoles);

                        if (role.Id == 1) {
                            // For the role with ID 1 we should get back one UserRoles result
                            Assert.Single (role.UserRoles);
                        } else {
                            // All other roles should return empty UserRoles "join" results
                            Assert.Empty (role.UserRoles);
                        }
                    }
                }

                sqlite.Close();
            }

            [Fact]
            public void TwoUserWithDifferentRoles_InMemory () {
                using(var writeContext = GetInMemoryContext(nameof(TwoUserWithDifferentRoles_InMemory))) {

                    InsertUsers(writeContext);
                    InsertRoles(writeContext);

                    writeContext.UserRoles.Add (new UserRoles () { UserId = 1, RoleId = 1 });
                    writeContext.UserRoles.Add (new UserRoles () { UserId = 2, RoleId = 2 });

                    writeContext.SaveChanges ();
                }

                using(var readContext = GetInMemoryContext(nameof(TwoUserWithDifferentRoles_InMemory))) {
                    var result = GetRolesByUser (readContext, 1);

                    // In total we expect 4 roles
                    Assert.Equal (2, result.Count);

                    foreach (var role in result) {
                        // The navigational property must be set
                        Assert.NotNull (role.UserRoles);

                        if (role.Id == 1) {
                            // For the role with ID 1 we should get back one UserRoles result
                            Assert.Single (role.UserRoles);
                        } else {
                            // All other roles should return empty UserRoles "join" results
                            Assert.Empty (role.UserRoles);
                        }
                    }
                }
            }

            [Fact]
            public void TwoUserWithDifferentRoles_PostgreSQL () {
                var connectionString = "Server=localhost; Port=5432; Database=postgres;User Id=postgres; Password=postgres;";
                using(var initContext = GetPostgreSQLContext(connectionString)) {
                    initContext.Database.EnsureCreated();

                    // EnsureDeleted didn't work for some reason, wiping data manually
                    initContext.UserRoles.RemoveRange(initContext.UserRoles);
                    initContext.Roles.RemoveRange(initContext.Roles);
                    initContext.Users.RemoveRange(initContext.Users);
                    initContext.SaveChanges();

                    Assert.Empty(initContext.UserRoles.ToList());
                }

                using(var writeContext = GetPostgreSQLContext(connectionString)) {

                    InsertUsers(writeContext);
                    InsertRoles(writeContext);

                    writeContext.UserRoles.Add (new UserRoles () { UserId = 1, RoleId = 1 });
                    writeContext.UserRoles.Add (new UserRoles () { UserId = 2, RoleId = 2 });

                    writeContext.SaveChanges ();
                }

                using(var readContext = GetPostgreSQLContext(connectionString)) {
                    var result = GetRolesByUser (readContext, 1);

                    // In total we expect 4 roles
                    Assert.Equal (2, result.Count);

                    foreach (var role in result) {
                        // The navigational property must be set
                        Assert.NotNull (role.UserRoles);

                        if (role.Id == 1) {
                            // For the role with ID 1 we should get back one UserRoles result
                            Assert.Single (role.UserRoles);
                        } else {
                            // All other roles should return empty UserRoles "join" results
                            Assert.Empty (role.UserRoles);
                        }
                    }
                }
            }

            public List<Role> GetRolesByUser (DemoContext context, int UserId) {
                var join = context.UserRoles.AsNoTracking().Where (ur => ur.UserId == UserId);

                var joinResult = join.ToList();
                var roleResult = context.Roles.ToList();

                var inMemoryGroupJoin = roleResult.GroupJoin(joinResult, outer => outer.Id, inner => inner.RoleId, (outer, inner) => new { Role = outer, Users = inner });
                var inMemoryResult = new List<Role>();

                foreach(var entry in inMemoryGroupJoin) {
                    var role = entry.Role;
                    role.UserRoles = entry.Users.ToList();

                    inMemoryResult.Add(role);
                }

                // Preferred way, but breaks the join-condition when EF Core decides to run two seperate queries (one for all UserRoles, one for all Roles)
                var preferredQuery = context.Roles.GroupJoin (
                    join,
                    outer => outer.Id,
                    inner => inner.RoleId,
                    (role, inner) => role
                ).Include (r => r.UserRoles);

                var preferredResult = preferredQuery.ToList ();

                return preferredResult;
            }

            public DemoContext GetInMemoryContext(string databaseName = null) {
                if(string.IsNullOrEmpty(databaseName)) {
                    // Pick a random databaseName if not specified
                    databaseName = Guid.NewGuid().ToString();
                }

                var options = new DbContextOptionsBuilder<DemoContext>()
                    .UseInMemoryDatabase(databaseName)
                    .UseLoggerFactory(MyLoggerFactory)
                    .EnableSensitiveDataLogging ()
                    .Options;

                return new DemoContext(options);
            }

            public DemoContext GetInMemorySqliteContext (SqliteConnection connection) {
                var options = new DbContextOptionsBuilder<DemoContext> ()
                    .UseSqlite (connection)
                    .UseLoggerFactory(MyLoggerFactory)
                    .EnableSensitiveDataLogging ()
                    .Options;

                var context = new DemoContext (options);

                return context;
            }

            public DemoContext GetPostgreSQLContext (string connectionString) {
                var options = new DbContextOptionsBuilder<DemoContext> ()
                    .UseNpgsql(connectionString)
                    .UseLoggerFactory(MyLoggerFactory)
                    .EnableSensitiveDataLogging ()
                    .Options;

                var context = new DemoContext (options);

                return context;
            }
        }
    }
}