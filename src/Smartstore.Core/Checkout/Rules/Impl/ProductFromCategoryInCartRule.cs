﻿using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Data;
using Smartstore.Core.Rules;

namespace Smartstore.Core.Checkout.Rules.Impl
{
    internal class ProductFromCategoryInCartRule : IRule
    {
        private readonly SmartDbContext _db;
        private readonly IShoppingCartService _shoppingCartService;

        public ProductFromCategoryInCartRule(SmartDbContext db, IShoppingCartService shoppingCartService)
        {
            _db = db;
            _shoppingCartService = shoppingCartService;
        }

        public async Task<bool> MatchAsync(CartRuleContext context, RuleExpression expression)
        {
            var categoryIds = Enumerable.Empty<int>();
            var cart = await _shoppingCartService.GetCartAsync(context.Customer, ShoppingCartType.ShoppingCart, context.Store.Id);
            var productIds = cart.Items.Select(x => x.Item.ProductId).ToArray();

            if (productIds.Any())
            {
                // It's unnecessary to check things like ACL, limited-to-stores, published, deleted etc. here
                // because the products are from shopping cart and it cannot contain hidden products.
                categoryIds = await _db.ProductCategories
                    .Where(x => productIds.Contains(x.ProductId))
                    .Select(x => x.CategoryId)
                    .ToListAsync();
            }

            var match = expression.HasListsMatch(categoryIds.Distinct());
            return match;
        }
    }
}
