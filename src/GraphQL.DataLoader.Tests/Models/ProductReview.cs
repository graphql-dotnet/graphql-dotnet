using System;
using System.Collections.Generic;
using System.Text;

namespace GraphQL.DataLoader.Tests.Models
{
    public class ProductReview
    {
        public int ProductReviewId { get; set; }
        public int ProductId { get; set; }
        public int UserId { get; set; }

        public int Rating { get; set; }
        public string Text { get; set; }
    }
}
