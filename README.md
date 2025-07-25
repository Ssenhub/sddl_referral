A. [API dataflow design](https://excalidraw.com/#json=Qs1pFdMCRMC4HVcXQKvRG,wsS-4QadhN5P_qBPmLxW8Q)

B. API specification:
   ~\sddl_referral\SddlReferralApiSpec.yaml

C. How to run unit tests:

1. Open SddlReferral.sln in Visual Studio 2022.
2. Click 'Test -> Run All Tests'

Unit test results can be viewed in Test Explorer (Click 'Test -> Test Explorer' to open it)

D. How to run functional tests:

1. Install PostgreSQL from https://www.postgresql.org/download if not already installed.
2. Open SddlReferral.sln in Visual Studio 2022.
3. Update "DefaultConnection" in appsettings.json accoding to local PostgreSQL setup.
4. Make sure "SddlReferral" is selected as Startup Item in VS.
5. Make sure  "http" is selected as launch configuration in VS.
6. Click Debug -> Start Without Debugging.
7. Open a Powershell window.
8. `>cd ~\sddl_referral\Tests\FunctionalTests`
9. `>.\Test-SddlReferral.ps1`

This should invoke all endpoints in order of the referral flow and validate the responses.

E. How to run each endpoints manually:

1. Follow steps 1 to 6 from section B.
2. Open file SddlReferral.http in VS
3. Click on 'Send request' link on each request. (Make sure inputs are changed accordingly)

F. How to generate token:
1. Visit https://jwt.io/ and click on 'JWT Encoder'
2. Enter a payload such as:
   `{
    "sub": "1234567890",
    "name": "John Doe",
    "admin": true,
    "iat": 1516239022
   }`
3. Update "JwtSecretKey" setting in appsettings.json to match with secret key under "Sign JWT: Secret" pane in jwt.io.
4. Copy the generated token under "JSON Web Token" and use it as 'Bearer' token in "Authorization" header.

