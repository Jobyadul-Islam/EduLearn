**EduLearn — Project Requirements**

 

# **1\. Project Overview**

EduLearn is an ASP.NET Core MVC e-learning platform where Instructors create and manage courses, Students enroll and learn, and Admins manage the overall platform.  
   
**2\. User Roles**  
 

| Role | Capabilities |
| :---- | :---- |
| **Admin** | Manage categories, approve instructors, manage all users, view platformwide reports |
| **Instructor** | Create/edit/delete their own courses, modules, lessons, assignments, quizzes; view enrolled students |
| **Student** | Browse/search courses, enroll, view lessons, submit assignments, take quizzes, view grades |

 

# **3\. Functional Requirements**

## **3.1 Authentication & Authorization**

•        Users can register as Student or Instructor (Admin created manually/seeded)

•        Login/Logout using ASP.NET Core Identity

•        Role-based access control (\[Authorize(Roles \= "...")\])

•        Instructor accounts require Admin approval before they can publish courses •       	Password reset / forgot password (optional but common requirement)  
 

 

 

 

 

## **3.2 Category Management (Admin)**

•        Create, edit, delete course categories

•        View list of all categories

 

 

## **3.3 Course Management (Instructor)**

•        Create/edit/delete courses (Title, Description, Price, Category)

•        Add Modules to a course

•        Add Lessons to a module (Title, Content, Video URL)

•        Publish/unpublish a course (draft vs. live)

•        View list of students enrolled in their course

## **3.4 Enrollment (Student)**

•        Browse all published courses

•        Search/filter courses by category or keyword

•        View course details before enrolling

•        Enroll in a course (free or paid — payment can be simulated/manual)

•        View "My Courses" dashboard

•        Track progress (e.g., % of lessons completed)

## **3.5 Learning Experience (Student)**

•        View lesson content and video within an enrolled course

•        Navigate between modules/lessons sequentially

•        Mark lessons as complete

## **3.6 Assignments**

•        Instructor creates assignments tied to a lesson (Title, Description, Due Date)

•        Student submits assignment (text or file upload)

•        Instructor views and grades submissions

 

 

## **3.7 Quizzes**

•        Instructor creates quizzes with multiple-choice questions

•        Student takes quiz, submits answers

•        System auto-grades and stores result (QuizResult)

•        Student views their quiz score/history

## **3.8 User Management (Admin)**

•        View all registered users

•        Approve/reject pending Instructor accounts

•        Activate/deactivate any user account

**3.9 Admin Dashboard**

•        Overview stats: total users, total courses, total enrollments (optional but common)

 

# **4\. Non-Functional Requirements**

| Category | Requirement |
| :---- | :---- |
| **Performance** | Pages should load within 2–3 seconds under normal use |
| **Security** | Passwords hashed via Identity; role-based authorization on all sensitive routes; input validation on all forms |
| **Usability** | Responsive design (Bootstrap) — usable on desktop and mobile |
| **Maintainability** | Follow MVC separation of concerns; consistent naming conventions |
| **Scalability** | Database designed to support growth in courses/users (proper indexing, foreign keys) |
| **Data Integrity** | Cascade delete rules configured correctly (e.g., deleting a Course removes its Modules/Lessons) |

 

 

 

 

# **5\. Database Entities**

Category ──\< Course ──\< Module ──\< Lesson ──\< Assignment

            	│                          	 

            	├──\< Enrollment \>── ApplicationUser (Student)

            	│

            	└──\< Quiz ──\< QuizQuestion

                   	       │

                        QuizResult \>── ApplicationUser (Student)

# **6\. Technology Stack**

•        **Backend:** ASP.NET Core MVC (.NET 8/10)

•        **Database:** SQL Server (via EF Core, Code-First)

•        **Auth:** ASP.NET Core Identity

•        **Frontend:** Razor Views \+ Bootstrap

•        **Version Control:** Git \+ GitHub

 

# **7\. Suggested Development Phases**

| Phase | Scope |
| :---- | :---- |
| 1 | Project setup, EF Core, database models |
| 2 | Identity, roles, authentication |
| 3 | Admin: Category CRUD |
| 4 | Instructor: Course/Module/Lesson CRUD |
| 5 | Student: Browse, Enroll, Learn |
| 6 | Assignments (create, submit, grade) |
| 7 | Quizzes (create, take, auto-grade) |
| 8 | Admin dashboard, user management, approval workflow |
| 9 | UI polish, responsive design, final testing |
| 10 | Deployment (optional) |

   
