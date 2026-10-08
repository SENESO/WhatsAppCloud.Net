# Integration Testing

The `test/WhatsAppCloud.IntegrationTests` project runs end-to-end tests against the
**real Meta WhatsApp Business Cloud API** (`https://graph.facebook.com`). These tests
send real messages, so they are fully opt-in: **when the credentials below are absent,
every test skips gracefully** (`Assert.Ignore`) and `dotnet test` reports green.

## Required environment variables

| Variable                   | Description                                                   |
|----------------------------|---------------------------------------------------------------|
| `WHATSAPP_ACCESS_TOKEN`    | A permanent page token or system-user token with `whatsapp_business_messaging` permission. |
| `WHATSAPP_PHONE_NUMBER_ID` | The phone number ID from the Meta developer console (digits, e.g. `1068...` — this is the ID, not the phone number itself). |
| `WHATSAPP_TEST_NUMBER`     | The recipient's WhatsApp number in E.164 format (digits only, e.g. `2010...`). |

Never hardcode these values in source. They are read from the process environment
only, at test time.

## Getting sandbox credentials

1. Open the Meta developer portal: https://developers.facebook.com/docs/whatsapp/cloud-api
2. Create an app (or open an existing one) and add the **WhatsApp** product.
3. In the WhatsApp > API Setup panel you get:
   - a **test phone number** and its **Phone number ID** (`WHATSAPP_PHONE_NUMBER_ID`),
   - a **temporary access token** (`WHATSAPP_ACCESS_TOKEN`). Temporary tokens expire
     after ~24 hours; for unattended runs generate a long-lived system-user token
     instead (Business Settings > System users > add token with
     `whatsapp_business_messaging`).
4. Add your personal WhatsApp number as a recipient in the **To** field — that is
   `WHATSAPP_TEST_NUMBER`.

The bundled `hello_world` template (language `en_US`) ships with every test number,
so the template test works without creating any templates. Free-form text messages
require an open 24-hour customer-service window; send the test number a message
first, or start with the template test.

## Running

```bash
export WHATSAPP_ACCESS_TOKEN="..."
export WHATSAPP_PHONE_NUMBER_ID="..."
export WHATSAPP_TEST_NUMBER="..."

dotnet test test/WhatsAppCloud.IntegrationTests -c Release
```

CI never runs these tests: the CI workflow sets no credentials, so every test
reports `Skipped` instead of failing.
