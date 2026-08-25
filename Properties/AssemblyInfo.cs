using System.Runtime.CompilerServices;

// Anonymous-typed query projections (e.g. InstructorController.QuizResults) are declared
// `internal` by the compiler, so integration tests in EduLearn.Tests can only read their
// properties via `dynamic` if this assembly explicitly grants that visibility.
[assembly: InternalsVisibleTo("EduLearn.Tests")]
