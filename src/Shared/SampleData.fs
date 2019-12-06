module SampleData

open Shared

let Users : Shared.Dto.UserList = [
    { username = "admin"
      firstName = "Admin"
      lastName = "User"
      emailAddresses = Some "admin@example.net"
      language = "en" }
    { username = "richard"
      firstName = "Richard"
      lastName = "Lionheart"
      emailAddresses = Some "king.richard@example.com"
      language = "en" }
    { username = "prince_john"
      firstName = "Prince"
      lastName = "John"
      emailAddresses = Some "princejohn@example.com"
      language = "en" }
    { username = "manager1"
      firstName = "Manager1"
      lastName = "User"
      emailAddresses = Some "manager1@example.net"
      language = "en" }
    { username = "manager2"
      firstName = "Manager2"
      lastName = "User"
      emailAddresses = Some "manager2@example.net"
      language = "en" }
    { username = "user1"
      firstName = "User"
      lastName = "One"
      emailAddresses = Some "user1@example.net"
      language = "en" }
    { username = "user2"
      firstName = "User"
      lastName = "Two"
      emailAddresses = Some "user2@example.net"
      language = "en" }
    { username = "Upper"
      firstName = "Upper"
      lastName = "Case"
      emailAddresses = Some "UPPER@example.net"
      language = "en" }
    { username = "modify"
      firstName = "Modify"
      lastName = "User"
      emailAddresses = Some "modify@example.net"
      language = "en" }
    { username = "test"
      firstName = "Test"
      lastName = "Palaso"
      emailAddresses = Some "Test@example.net"
      language = "en" }
    { username = "tuck"
      firstName = "Friar"
      lastName = "Tuck"
      emailAddresses = Some "friar_tuck@example.org"
      language = "en" }
    { username = "guest"
      firstName = "Guest"
      lastName = "Observer"
      emailAddresses = Some "noone@example.com"
      language = "en" }
    { username = "guest-palaso"
      firstName = "Guest"
      lastName = "Palaso"
      emailAddresses = Some "nobody@example.org"
      language = "en" }
    { username = "rhood"
      firstName = "Robin"
      lastName = "Hood"
      emailAddresses = Some "rhood@example.com"
      language = "en" }
    { username = "willscarlet"
      firstName = "Will"
      lastName = "Scarlet"
      emailAddresses = Some "ws1@example.org"
      language = "en" }
    { username = "adale"
      firstName = "Alan"
      lastName = "a Dale"
      emailAddresses = Some "alan_a_dale@example.org"
      language = "en" }
]

let Projects : Shared.Dto.ProjectList = [

    { code = "ld-test"
      name = "LD Test"
      description = "LD API Test project"
      membership = Some [] }

    { code = "test-ld-dictionary"
      name = "LD Test Dictionary"
      description = "LD API Test Dictionary project"
      membership = Some ["manager1", Manager; "user1", Contributor; "test", Contributor] }

    { code = "test-ld-flex"
      name = "LD API Test Flex"
      description = "LD API Test FLEx project"
      membership = Some ["manager2", Manager; "user2", Contributor; "test", Manager] }

    { code = "test-ld-demo"
      name = "LD API Test Demo"
      description = "LD API Test Demo project"
      membership = Some ["test", Programmer; "test", Manager] }

    { code = "test-ld-adapt"
      name = "LD API Test AdaptIT"
      description = "LD API Test AdaptIT project"
      membership = Some [] }

    { code = "test-ld-training"
      name = "LD API Test Training"
      description = "LD API Test Training project"
      membership = Some [] }

    { code = "test-ld-ütf8"
      name = "LD API UTF8 Eñcoding"
      description = "LD API Test UTF8 Eñcoding project"
      membership = Some ["test", Manager] }

    { code = "tha-food"
      name = "Thai Food Dictionary"
      description = "A picture dictionary of Thai food."
      membership = Some ["richard", Manager; "tuck", Manager; "user1", Contributor; "guest-palaso", Observer; "rhood", Programmer] }

    { code = "test-sherwood-sena-03"
      name = "Sherwood TestSena3 03"
      description = ""
      membership = Some ["willscarlet", Manager; "rhood", Manager; "tuck", Contributor] }

    { code = "test-ws-1-flex"
      name = "test-ws-1-flex"
      description = ""
      membership = Some ["willscarlet", Manager] }

    { code = "robin-test-projects"
      name = "Robin Test Projects"
      description = "Test projects for Robin Hood's testing of Send/Receive scenarios"
      membership = Some ["rhood", Manager] }

    { code = "test-robin-flex-new-public"
      name = "Robin Test FLEx new public"
      description = ""
      membership = Some ["rhood", Manager] }

    { code = "test-robin-new-public-2"
      name = "Robin new public 2"
      description = ""
      membership = Some ["rhood", Manager] }

    { code = "alan_test"
      name = "Alan_test"
      description = "To test hg pull/push"
      membership = Some ["adale", Manager] }

    { code = "aland_test"
      name = "aland_test"
      description = "hg pull/push tests"
      membership = Some ["adale", Manager] }

    { code = "tha-food2"
      name = "tha-food2"
      description = ""
      membership = Some ["tuck", Manager] }

]
