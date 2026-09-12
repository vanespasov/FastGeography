Feature: Geography Answer Validation
  In order to receive fair points for my geography answers
  As a game player
  I want the server to validate my answers and return a typed result

  Scenario: Known city earns full points
    When I submit "London" as location type "City"
    Then the response is successful
    And the awarded points are 20
    And the response includes coordinates

  Scenario: Unrecognised location loses points
    When I submit "XYZNOTEXIST" as location type "City"
    Then the response is successful
    And the awarded points are -5
    And the response has no coordinates

  Scenario: Answer longer than 100 characters is rejected
    Given a location name 101 characters long
    When I submit that overlong location as location type "City"
    Then the request is rejected with status code 400

  Scenario: Unrecognised location type is rejected
    When I submit "London" as location type "NotAType"
    Then the request is rejected with status code 400

  Scenario Outline: Multiple known places earn full points
    When I submit "<location>" as location type "<locationType>"
    Then the response is successful
    And the awarded points are 20

    Examples:
      | location | locationType |
      | Paris    | City         |
      | Berlin   | City         |
      | Sofia    | Country      |
      | Tokyo    | City         |
