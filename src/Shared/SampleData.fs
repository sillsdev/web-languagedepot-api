module SampleData

open Shared

let StandardRoles : Shared.Dto.RoleDetails[] = [|
    { name = "Manager"
      id = 3 }
    { name = "Contributor"
      id = 4 }
    { name = "Observer"
      id = 5 }
    { name = "LanguageDepotProgrammer"
      id = 6 }
|]

let Users : Shared.Dto.UserList = [|
    { username = "admin"
      firstName = "Admin"
      lastName = "User"
      email = Some "admin@example.net"
      language = "en" }
    { username = "richard"
      firstName = "Richard"
      lastName = "Lionheart"
      email = Some "king.richard@example.com"
      language = "en" }
    { username = "prince_john"
      firstName = "Prince"
      lastName = "John"
      email = Some "princejohn@example.com"
      language = "en" }
    { username = "manager1"
      firstName = "Manager1"
      lastName = "User"
      email = Some "manager1@example.net"
      language = "en" }
    { username = "manager2"
      firstName = "Manager2"
      lastName = "User"
      email = Some "manager2@example.net"
      language = "en" }
    { username = "user1"
      firstName = "User"
      lastName = "One"
      email = Some "user1@example.net"
      language = "en" }
    { username = "user2"
      firstName = "User"
      lastName = "Two"
      email = Some "user2@example.net"
      language = "en" }
    { username = "Upper"
      firstName = "Upper"
      lastName = "Case"
      email = Some "UPPER@example.net"
      language = "en" }
    { username = "modify"
      firstName = "Modify"
      lastName = "User"
      email = Some "modify@example.net"
      language = "en" }
    { username = "test"
      firstName = "Test"
      lastName = "Palaso"
      email = Some "Test@example.net"
      language = "en" }
    { username = "tuck"
      firstName = "Friar"
      lastName = "Tuck"
      email = Some "friar_tuck@example.org"
      language = "en" }
    { username = "guest"
      firstName = "Guest"
      lastName = "Observer"
      email = Some "noone@example.com"
      language = "en" }
    { username = "guest-palaso"
      firstName = "Guest"
      lastName = "Palaso"
      email = Some "nobody@example.org"
      language = "en" }
    { username = "rhood"
      firstName = "Robin"
      lastName = "Hood"
      email = Some "rhood@example.com"
      language = "en" }
    { username = "willscarlet"
      firstName = "Will"
      lastName = "Scarlet"
      email = Some "ws1@example.org"
      language = "en" }
    { username = "adale"
      firstName = "Alan"
      lastName = "a Dale"
      email = Some "alan_a_dale@example.org"
      language = "en" }
|]

let Projects : Shared.Dto.ProjectList = [|

    { code = "ld-test"
      name = "LD Test"
      description = "LD API Test project"
      membership = Map.empty }

    { code = "test-ld-dictionary"
      name = "LD Test Dictionary"
      description = "LD API Test Dictionary project"
      membership = Map.ofList ["manager1", "Manager"; "user1", "Contributor"; "test", "Contributor"] }

    { code = "test-ld-flex"
      name = "LD API Test Flex"
      description = "LD API Test FLEx project"
      membership = Map.ofList ["manager2", "Manager"; "user2", "Contributor"; "test", "Manager"] }

    { code = "test-ld-demo"
      name = "LD API Test Demo"
      description = "LD API Test Demo project"
      membership = Map.ofList ["test", "LanguageDepotProgrammer"; "test", "Manager"] }

    { code = "test-ld-adapt"
      name = "LD API Test AdaptIT"
      description = "LD API Test AdaptIT project"
      membership = Map.empty }

    { code = "test-ld-training"
      name = "LD API Test Training"
      description = "LD API Test Training project"
      membership = Map.empty }

    { code = "test-ld-ütf8"
      name = "LD API UTF8 Eñcoding"
      description = "LD API Test UTF8 Eñcoding project"
      membership = Map.ofList ["test", "Manager"] }

    { code = "tha-food"
      name = "Thai Food Dictionary"
      description = "A picture dictionary of Thai food."
      membership = Map.ofList ["richard", "Manager"; "tuck", "Manager"; "user1", "Contributor"; "guest-palaso", "Observer"; "rhood", "LanguageDepotProgrammer"] }

    { code = "test-sherwood-sena-03"
      name = "Sherwood TestSena3 03"
      description = ""
      membership = Map.ofList ["willscarlet", "Manager"; "rhood", "Manager"; "tuck", "Contributor"] }

    { code = "test-ws-1-flex"
      name = "test-ws-1-flex"
      description = ""
      membership = Map.ofList ["willscarlet", "Manager"] }

    { code = "robin-test-projects"
      name = "Robin Test Projects"
      description = "Test projects for Robin Hood's testing of Send/Receive scenarios"
      membership = Map.ofList ["rhood", "Manager"] }

    { code = "test-robin-flex-new-public"
      name = "Robin Test FLEx new public"
      description = ""
      membership = Map.ofList ["rhood", "Manager"] }

    { code = "test-robin-new-public-2"
      name = "Robin new public 2"
      description = ""
      membership = Map.ofList ["rhood", "Manager"] }

    { code = "alan_test"
      name = "Alan_test"
      description = "To test hg pull/push"
      membership = Map.ofList ["adale", "Manager"] }

    { code = "aland_test"
      name = "aland_test"
      description = "hg pull/push tests"
      membership = Map.ofList ["adale", "Manager"] }

    { code = "tha-food2"
      name = "tha-food2"
      description = ""
      membership = Map.ofList ["tuck", "Manager"] }

|]
