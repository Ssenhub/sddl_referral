How to run unit tests:
1. Open SddlReferral.sln in Visual Studio 2022.
2. Click Test -> Run All Tests

Unit test results can be viewed in Test Explorer (Client Test -> Test Explorer to open it)

1. Install PostgreSQL from https://www.postgresql.org/download.
2. Open SddlReferral.sln in Visual Studio 2022.
3. Update "DefaultConnection" in appsettings.json accodingly.
4. Make sure "SddlReferral" is selected as Startup Item in VS.
5. Make sure  "http" is selected as launch configuration in VS.
6. Click Debug -> Start Without Debugging.
7. Open a Powershell window.
8. > cd ~\sddl_referral\Tests\FunctionalTests
9. >.\Test-SddlReferral.ps1

This should invoke all endpoints in order of the referral flow and validate the responses.

https://excalidraw.com/#json=5rhGnIJb-Goj0z2Oux59j,ckv0TrUPZXKPqKiTqFOFEQ
