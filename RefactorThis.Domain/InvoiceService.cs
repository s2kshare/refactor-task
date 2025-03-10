using RefactorThis.Persistence;
using System;
using System.Linq;

namespace RefactorThis.Domain
{
    public class InvoiceService
    {
        private readonly InvoiceRepository _invoiceRepository;

        public InvoiceService(InvoiceRepository invoiceRepository)
        {
            _invoiceRepository = invoiceRepository;
        }

        /// <summary>
        /// Processes a payment by adding it to an invoice.
        /// </summary>
        /// <param name="payment">The payment to be processed.</param>
        /// <returns>A response message indicating the status of the payment in relation to the invoice.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the invoice is null or has invalid state.</exception>
        public string ProcessPayment(Payment payment)
        {
            var invoice = _invoiceRepository.GetInvoice(payment.Reference);

            if (invoice == null)
                throw new InvalidOperationException("There is no invoice matching this payment");

            // Check if the invoice has an amount due
            if (invoice.Amount == 0)
            {
                // Invoice has no amount due, so no payment is needed
                return invoice.Payments == null || !invoice.Payments.Any() ? "no payment needed" : 
                    // Invoice has payments but no amount due, which is an invalid state
                    "The invoice is in an invalid state, it has an amount of 0 and it has payments.";
            }

            // Generate a response message based on the invoice status and payment
            var responseMessage = GenerateResponse(invoice, payment);

            // Apply the payment to the invoice
            ApplyPaymentToInvoice(invoice, payment);

            return responseMessage;
        }

        /// <summary>
        /// Generates a response message based on the payment information and invoice status.
        /// </summary>
        /// <param name="invoice">The invoice related to the payment.</param>
        /// <param name="payment">The payment being processed.</param>
        /// <returns>A response message indicating the status of the payment in relation to the invoice.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the invoice is null.</exception>
        private string GenerateResponse(Invoice invoice, Payment payment)
        {
            if (invoice == null)
                throw new InvalidOperationException("There is no invoice matching this payment");

            // Check if there are existing payments on the invoice
            if (invoice.Payments != null && invoice.Payments.Any())
            {
                // Invoice is already fully paid
                if (invoice.Payments.Sum(x => x.Amount) != 0 && invoice.Amount == invoice.Payments.Sum(x => x.Amount))
                {
                    return "invoice was already fully paid";
                }
                // Payment exceeds remaining amount due
                else if (invoice.Payments.Sum(x => x.Amount) != 0 && payment.Amount > (invoice.Amount - invoice.AmountPaid))
                {
                    return "the payment is greater than the partial amount remaining";
                }
                else
                {
                    // Determine if the payment completes the invoice or is a partial payment
                    return (invoice.Amount - invoice.AmountPaid) == payment.Amount
                        ? "final partial payment received, invoice is now fully paid"
                        : "another partial payment received, still not fully paid";
                }
            }
            else
            {
                // No existing payments, check if the payment exceeds or matches the invoice amount
                if (payment.Amount > invoice.Amount)
                {
                    return "the payment is greater than the invoice amount";
                }
                else if (invoice.Amount == payment.Amount)
                {
                    return "invoice is now fully paid";
                }
                else
                {
                    return "invoice is now partially paid";
                }
            }
        }

        /// <summary>
        /// Applies a payment to an invoice, updating the invoice's paid amount, tax amount, 
        /// and adding the payment to the list of payments based on the invoice type.
        /// </summary>
        /// <param name="invoice">The invoice to which the payment is applied.</param>
        /// <param name="payment">The payment to be applied to the invoice.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the invoice type is unknown.</exception>
        private void ApplyPaymentToInvoice(Invoice invoice, Payment payment)
        {
            // Determine the behavior based on the invoice type
            switch (invoice.Type)
            {
                case InvoiceType.Standard:
                    // Update the amount paid and tax amount for standard invoices
                    invoice.AmountPaid += payment.Amount;
                    invoice.TaxAmount += payment.Amount * 0.14m;
                    invoice.Payments.Add(payment);
                    break;
                case InvoiceType.Commercial:
                    // Update the amount paid and tax amount for commercial invoices
                    invoice.AmountPaid += payment.Amount;
                    invoice.TaxAmount += payment.Amount * 0.14m;
                    invoice.Payments.Add(payment);
                    break;
                default:
                    // Handle unexpected invoice types
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}