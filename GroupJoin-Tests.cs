using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Xunit;
using System.Linq;
using System;
using GroupJoinTest.Models;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace GroupJoinTest
{
    namespace Test
    {
        public class GroupJoinTest {
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
            public void TwoUserWithDifferentRoles () {
                using(var writeContext = GetInMemoryContext(nameof(TwoUserWithDifferentRoles))) {

                    InsertUsers(writeContext);
                    InsertRoles(writeContext);

                    writeContext.UserRoles.Add (new UserRoles () { UserId = 1, RoleId = 1 });
                    writeContext.UserRoles.Add (new UserRoles () { UserId = 2, RoleId = 2 });

                    writeContext.SaveChanges ();
                }

                using(var readContext = GetInMemoryContext(nameof(TwoUserWithDifferentRoles))) {
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
            public void TwoUserWithSameRoles () {
                using(var writeContext = GetInMemoryContext(nameof(TwoUserWithSameRoles))) {

                    InsertUsers(writeContext);
                    InsertRoles(writeContext);

                    writeContext.UserRoles.Add (new UserRoles () { UserId = 1, RoleId = 1 });
                    writeContext.UserRoles.Add (new UserRoles () { UserId = 2, RoleId = 1 });

                    writeContext.SaveChanges ();
                }

                using(var readContext = GetInMemoryContext(nameof(TwoUserWithSameRoles))) {
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
            public void TwoUsersWithAllRolesEach () {
                using(var writeContext = GetInMemoryContext(nameof(TwoUsersWithAllRolesEach))) {

                    InsertUsers(writeContext);
                    InsertRoles(writeContext);

                    writeContext.UserRoles.Add (new UserRoles () { UserId = 1, RoleId = 1 });
                    writeContext.UserRoles.Add (new UserRoles () { UserId = 1, RoleId = 2 });
                    writeContext.UserRoles.Add (new UserRoles () { UserId = 2, RoleId = 1 });
                    writeContext.UserRoles.Add (new UserRoles () { UserId = 2, RoleId = 2 });

                    writeContext.SaveChanges ();
                }

                using(var readContext = GetInMemoryContext(nameof(TwoUsersWithAllRolesEach))) {
                    var result = GetRolesByUser (readContext, 1);

                    // In total we expect 4 roles
                    Assert.Equal (2, result.Count);

                    foreach (var role in result) {
                        // The navigational property must be set
                        Assert.NotNull (role.UserRoles);
                        Assert.Single (role.UserRoles);
                    }
                }
            }

            [Fact]
            public void TestUserWithoutRolesOtherUserWithAllRoles () {
                using(var writeContext = GetInMemoryContext(nameof(TestUserWithoutRolesOtherUserWithAllRoles))) {

                    InsertUsers(writeContext);
                    InsertRoles(writeContext);

                    writeContext.UserRoles.Add (new UserRoles () { UserId = 2, RoleId = 1 });
                    writeContext.UserRoles.Add (new UserRoles () { UserId = 2, RoleId = 2 });

                    writeContext.SaveChanges ();
                }

                using(var readContext = GetInMemoryContext(nameof(TestUserWithoutRolesOtherUserWithAllRoles))) {
                    var result = GetRolesByUser (readContext, 1);

                    // In total we expect 4 roles
                    Assert.Equal (2, result.Count);

                    foreach (var role in result) {
                        // The navigational property must be set
                        Assert.NotNull (role.UserRoles);
                        Assert.Empty (role.UserRoles);
                    }
                }
            }

            [Fact]
            public void TestUserWithoutRolesOtherUserWithOneRole () {
                using(var writeContext = GetInMemoryContext(nameof(TestUserWithoutRolesOtherUserWithOneRole))) {

                    InsertUsers(writeContext);
                    InsertRoles(writeContext);

                    writeContext.UserRoles.Add (new UserRoles () { UserId = 2, RoleId = 1 });

                    writeContext.SaveChanges ();
                }

                using(var readContext = GetInMemoryContext(nameof(TestUserWithoutRolesOtherUserWithOneRole))) {
                    var result = GetRolesByUser (readContext, 1);

                    // In total we expect 4 roles
                    Assert.Equal (2, result.Count);

                    foreach (var role in result) {
                        // The navigational property must be set
                        Assert.NotNull (role.UserRoles);
                        Assert.Empty (role.UserRoles);
                    }
                }
            }

            //[Fact]
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

            [Fact]
            public void TwoUserWithDifferentRoles_SQLite () {
                var sqlite = new SqliteConnection("DataSource=:memory:");
                sqlite.Open();

                using(var writeContext = GetInMemorySqliteContext(sqlite)) {
                    writeContext.Database.EnsureCreated();

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

            public List<Role> GetRolesByUser (DemoContext context, int UserId) {
                // Query the database
                var preferredQuery = context.Roles.GroupJoin (
                        context.UserRoles,
                        outer => outer.Id,
                        inner => inner.RoleId,
                        (role, inner) => role
                    )
                    .SelectMany(
                        collectionSelector: role => role.UserRoles.Where(ur => ur.UserId == UserId).DefaultIfEmpty(),
                        resultSelector: (role, userRole) => new { Role = role, UserRole = userRole }
                    );

                // Build the right result structure.
                // I havent found way on how to populate the navigational property in any other way...
                var result = new List<Role>();
                foreach(var entry in preferredQuery.ToList ()) {
                    var role = entry.Role;
                    role.UserRoles = new List<UserRoles>();
                    if(entry.UserRole != null) {
                        role.UserRoles.Add(entry.UserRole);
                    }
                    result.Add(role);
                }

                return result;
            }
            // Helpers to create the needed contexts
            public DemoContext GetInMemoryContext(string databaseName = null) {
                if(string.IsNullOrEmpty(databaseName)) {
                    // Pick a random databaseName if not specified
                    databaseName = Guid.NewGuid().ToString();
                }

                var options = new DbContextOptionsBuilder<DemoContext>()
                    .UseInMemoryDatabase(databaseName)
                    // .UseLoggerFactory(MyLoggerFactory)
                    // .EnableSensitiveDataLogging ()
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

            // Various Utility Functions
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
        }
    }
}