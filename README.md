# get-npm-package-dependencies
A WebAPI which receives a package name and returns its dependencies tree

An example of calling the api: https://localhost/api/Dependencies?packageName=react-native&packageVersion=0.63.2

Tests:
1. Empty package name on input will result a proper message.
2. A package that doesn't exist will result a proper message.
3. Checking that there is a valid result on known packages (the result itself can change over time so checking it might result wrong answer) 
4. Checking that fetching the result doesn't take more than X seconds (to make sure we don't get into circular dependencies).
5. Empty version on input will result latest version.
6. Checking that the result format is the expected Json object format.
