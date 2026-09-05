# AutoShop and TPGLLC customer portal

AutoShop remains the source of repair work, inventory, and payments. Staff explicitly publish saved repairs to the website using **Publish to customer portal**. The website shows a repair timeline, the customer message, parts, and the current estimate. Publishing does not email customers.

## Configure the website first

Deploy the matching `TPGLLC_WebSite` integration branch to your existing ASP.NET Core / SQL Server environment. Set these server-side configuration values outside Git:

| Setting | Value |
| --- | --- |
| `AutoShop:ApiKey` (environment variable `AutoShop__ApiKey`) | A randomly generated secret, 32–512 characters |
| `AutoShop:SourceId` (`AutoShop__SourceId`) | One new GUID identifying this AutoShop database |

Generate the source GUID once and retain it in backups. Changing it creates a different repair namespace. One website configuration currently supports one shop database. Do not share the same source namespace between unrelated AutoShop databases; local work-order IDs can overlap. The endpoint stays inaccessible until the key and source are configured. Use HTTPS with a valid certificate. When using IIS, retain ASP.NET Core Module HTTPS forwarding; other proxies must securely forward the original scheme.

The implementation uses existing service-history tables; it adds no database columns. The target website database must already have its normal complete schema, including service history, parts, and updates. It does not bootstrap a new website database.

The shop key permits listing portal customers and publishing repairs. Treat it as an administrative connection secret, distribute it only to trusted shop staff, and rotate it on the website and desktop together when necessary. Do not put it in this repository or a public URL.

## Publish a repair

1. Create the customer's portal account and customer record through the website's existing registration/administration workflow. The customer record must be linked to the login account.
2. Sign in to AutoShop as an administrator or technician. Save the work order, status, parts, and internal notes as usual.
3. Open **Publish to customer portal**. Enter the website's HTTPS address and connection key, then connect. Optional desktop environment variables `Portal__BaseUrl` and `Portal__ApiKey` prefill these fields; credentials entered in the window are not saved to disk.
4. Select the saved work order. On first publication, explicitly select and verify the customer's name and email. Type the message the customer should see and choose **Publish repair update**.
5. The customer signs into the website and opens `/portal/customers/work-orders`. **Refresh repair updates** loads the latest publication.

After publishing, the website retains the customer link and latest customer message. Reopening the publishing screen loads those values. A published repair cannot be reassigned to another customer through the API. Contact the administrator to correct an erroneous first-time link; there is no automatic email-based linking.

Publishing is manual, not continuous background synchronization. Save and publish again after a repair changes. Network failures leave local repair data saved, display an error, and can be retried. Repeating an unchanged publication does not duplicate the repair, parts, or timeline. Only the explicitly entered customer message is public; existing internal notes, diagnosis, customer notes, and credentials are excluded from the payload.

## Staff accounts and roles

Administrators open **Staff accounts and roles** to create/edit accounts, reset passwords, assign multiple roles, and deactivate staff. New passwords entered through this screen must have at least 12 characters. Change the repository's default administrator password before production use.

| Role | Writes allowed through the application |
| --- | --- |
| Administrator | All shop records and staff accounts |
| Technician | Customers, vehicles, repairs, parts, inventory, and portal publication; not staff settings or payment amounts |
| Finance | Payment amounts, balances, and completion-to-paid transitions; not repairs, inventory, or staff settings |

Roles may be combined. The scoped database context checks writes against the current active user's stored role. Staff still share access to the desktop's shop views; these roles do not prevent someone with direct filesystem access from opening SQLite. Customer accounts and customer access controls remain on the website; Windows staff passwords are not copied there.

## Boundaries of this release

- This is repair tracking and publication, not two-way work-order editing. Imported repairs are updated from AutoShop. Customer approvals for imported repairs require contacting the shop; the portal does not claim to send a decision back to AutoShop.
- Payments, final invoices, appointment intake, customer account creation, and inventory quantities are not synchronized. Existing website-native billing and appointment features remain separate. The published amount is an estimate, not a payment request.
- The bridge does not merge pre-existing website repair records with desktop repairs. Initial publication creates a linked repair record in its own stable namespace.
- No production deployment, production SQL Server validation, installer signing, or live customer walkthrough is implied by the automated test results.

## Verify

Run `dotnet test AutoShop.Tests.Integration/AutoShop.Tests.Integration.csproj`, `dotnet test AutoShop.Tests.UI/AutoShop.Tests.UI.csproj`, and `dotnet build AutoShop.UI/AutoShop.UI.csproj` from this repository. The matching website has `TPGLLC.Integration.Tests/TPGLLC.Integration.Tests.csproj` for HTTP authorization, repeat publishing, validation, and customer linking checks. Test database helpers explicitly check their temporary database path before migrations or writes.
