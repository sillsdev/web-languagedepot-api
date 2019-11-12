module SampleData

let Users : Shared.Dto.UserList = [
    { username = "admin"
      firstName = "Admin"
      lastName = "User"
      emailAddresses = ["admin@example.net"]
      language = "en" }
    { username = "richard"
      firstName = "Richard"
      lastName = "Lionheart"
      emailAddresses = ["king.richard@example.com"]
      language = "en" }
    { username = "prince_john"
      firstName = "Prince"
      lastName = "John"
      emailAddresses = ["princejohn@example.com"]
      language = "en" }
    { username = "manager1"
      firstName = "Manager1"
      lastName = "User"
      emailAddresses = ["manager1@example.net"]
      language = "en" }
    { username = "manager2"
      firstName = "Manager2"
      lastName = "User"
      emailAddresses = ["manager2@example.net"]
      language = "en" }
    { username = "user1"
      firstName = "User"
      lastName = "One"
      emailAddresses = ["user1@example.net"]
      language = "en" }
    { username = "user2"
      firstName = "User"
      lastName = "Two"
      emailAddresses = ["user2@example.net"]
      language = "en" }
    { username = "Upper"
      firstName = "Upper"
      lastName = "Case"
      emailAddresses = ["UPPER@example.net"]
      language = "en" }
    { username = "modify"
      firstName = "Modify"
      lastName = "User"
      emailAddresses = ["modify@example.net"]
      language = "en" }
    { username = "test"
      firstName = "Test"
      lastName = "Palaso"
      emailAddresses = ["Test@example.net"]
      language = "en" }
    { username = "tuck"
      firstName = "Friar"
      lastName = "Tuck"
      emailAddresses = ["friar_tuck@example.org"]
      language = "en" }
    { username = "guest"
      firstName = "Guest"
      lastName = "Observer"
      emailAddresses = ["noone@example.com"]
      language = "en" }
    { username = "guest-palaso"
      firstName = "Guest"
      lastName = "Palaso"
      emailAddresses = ["nobody@example.org"]
      language = "en" }
    { username = "rhood"
      firstName = "Robin"
      lastName = "Hood"
      emailAddresses = ["rhood@example.com";"robin_hood@example.org"]
      language = "en" }
    { username = "willscarlet"
      firstName = "Will"
      lastName = "Scarlet"
      emailAddresses = ["ws1@example.org"]
      language = "en" }
    { username = "adale"
      firstName = "Alan"
      lastName = "a Dale"
      emailAddresses = ["alan_a_dale@example.org"]
      language = "en" }
]

let Projects : Shared.Dto.ProjectList = [

    { code = "ld-test"
      name = "LD Test"
      description = "LD API Test project"
      membership = Some {
          managers = []
          contributors = []
          observers = []
          programmers = [] } }

    { code = "test-ld-dictionary"
      name = "LD Test Dictionary"
      description = "LD API Test Dictionary project"
      membership = Some {
          managers = ["manager1"]
          contributors = ["user1"; "test"]
          observers = []
          programmers = [] } }

    { code = "test-ld-flex"
      name = "LD API Test Flex"
      description = "LD API Test FLEx project"
      membership = Some {
          managers = ["manager2"; "test"]
          contributors = ["user2"]
          observers = []
          programmers = [] } }

    { code = "test-ld-demo"
      name = "LD API Test Demo"
      description = "LD API Test Demo project"
      membership = Some {
          managers = ["test"]
          contributors = []
          observers = []
          programmers = ["test"] } }

    { code = "test-ld-adapt"
      name = "LD API Test AdaptIT"
      description = "LD API Test AdaptIT project"
      membership = Some {
          managers = []
          contributors = []
          observers = []
          programmers = [] } }

    { code = "test-ld-training"
      name = "LD API Test Training"
      description = "LD API Test Training project"
      membership = Some {
          managers = []
          contributors = []
          observers = []
          programmers = [] } }

    { code = "test-ld-ütf8"
      name = "LD API UTF8 Eñcoding"
      description = "LD API Test UTF8 Eñcoding project"
      membership = Some {
          managers = ["test"]
          contributors = []
          observers = []
          programmers = [] } }

    { code = "tha-food"
      name = "Thai Food Dictionary"
      description = "A picture dictionary of Thai food."
      membership = Some {
          managers = ["richard"; "tuck"]
          contributors = ["user1"]
          observers = ["guest-palaso"]
          programmers = ["rhood"] } }

    { code = "test-sherwood-sena-03"
      name = "Sherwood TestSena3 03"
      description = ""
      membership = Some {
          managers = ["willscarlet"; "rhood"]
          contributors = ["tuck"]
          observers = []
          programmers = [] } }

    { code = "test-ws-1-flex"
      name = "test-ws-1-flex"
      description = ""
      membership = Some {
          managers = ["willscarlet"]
          contributors = []
          observers = []
          programmers = [] } }

    { code = "robin-test-projects"
      name = "Robin Test Projects"
      description = "Test projects for Robin Hood's testing of Send/Receive scenarios"
      membership = Some {
          managers = ["rhood"]
          contributors = []
          observers = []
          programmers = [] } }

    { code = "test-robin-flex-new-public"
      name = "Robin Test FLEx new public"
      description = ""
      membership = Some {
          managers = ["rhood"]
          contributors = []
          observers = []
          programmers = [] } }

    { code = "test-robin-new-public-2"
      name = "Robin new public 2"
      description = ""
      membership = Some {
          managers = ["rhood"]
          contributors = []
          observers = []
          programmers = [] } }

    { code = "alan_test"
      name = "Alan_test"
      description = "To test hg pull/push"
      membership = Some {
          managers = ["adale"]
          contributors = []
          observers = []
          programmers = [] } }

    { code = "aland_test"
      name = "aland_test"
      description = "hg pull/push tests"
      membership = Some {
          managers = ["adale"]
          contributors = []
          observers = []
          programmers = [] } }

    { code = "tha-food2"
      name = "tha-food2"
      description = ""
      membership = Some {
          managers = ["tuck"]
          contributors = []
          observers = []
          programmers = [] } }

]
