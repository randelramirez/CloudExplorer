# Cloud API cost posture

Cloud Explorer performs no background polling. After a provider's initial view load, resource calls occur only when the user refreshes or explicitly changes the selected account scope.

## AWS

AWS Resource Explorer has no additional charge for searches. AWS notes that building inventory and optional integrations can call APIs that have their own pricing. This application does not use AWS Config, Cost Explorer, or a service-specific enrichment API. AWS documentation: <https://aws.amazon.com/resourceexplorer/pricing/>.

## Azure

The application uses Azure Resource Manager's resource-list control-plane operation through Azure CLI. Resource-list and authentication operations are not separately billed, but they are subject to Azure request throttling. The application does not use Log Analytics, paid monitoring ingestion, or continuous change tracking.

## Future changes

Any service-specific enricher must be documented with its pricing and throttling behavior. Potentially billable enrichment must be opt-in and may execute only as part of an explicit refresh.
