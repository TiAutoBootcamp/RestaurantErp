﻿using RestaurantErp.Core.Contracts;
using RestaurantErp.Core.Extensions;
using RestaurantErp.Core.Models.Discount;
using RestaurantErp.Core.Models.Order;
using System.Collections.Concurrent;

namespace RestaurantErp.Core.Providers
{
    public class DiscountByTimeProvider : IDiscountByTimeProvider
    {
        private readonly ConcurrentDictionary<Guid, DiscountByTimeSettings> _discountById = new ConcurrentDictionary<Guid, DiscountByTimeSettings>();
        private readonly DiscountByTimeProviderSettings _settings;

        public DiscountByTimeProvider(DiscountByTimeProviderSettings settings)
        {
            _settings = settings;
        }

        public Guid Add(DiscountByTimeSettings settings)
        {
            var id = Guid.NewGuid();

            _discountById[id] = settings;

            return id;
        }

        public bool Remove(Guid id) => _discountById.TryRemove(id, out var _);

        // TODO:
        // change to return deepcopy
        public IDictionary<Guid, DiscountByTimeSettings> GetAll() => _discountById.DeepCopy();

        public BillDiscountInfo Calculate(Order order)
        {
            var t = order.Items
                .ToDictionary(
                    orderItem => orderItem,
                    orderItem => _discountById.Values.Where(discountInfo => discountInfo.StartTime <= TimeOnly.FromDateTime(orderItem.OrderingTime)
                        && discountInfo.EndTime.Add(_settings.EndDiscountDelay) >= TimeOnly.FromDateTime(orderItem.OrderingTime)
                        && orderItem.ProductId == discountInfo.ProductId))
                .Where(appliedDiscountsByOrderItem => appliedDiscountsByOrderItem.Value.Count() > 0).ToArray();

            var billDiscountItemInfo = t
                .ToDictionary(appliedDiscountsByOrderItem => appliedDiscountsByOrderItem.Key, appliedDiscountsByOrderItem => appliedDiscountsByOrderItem.Value.Sum(i => i.DiscountValue))
                .Select(discountsSumRateByOrderItem => new BillDiscountItemInfo
                {
                    ItemId = discountsSumRateByOrderItem.Key.ItemId,
                    DiscountAmount = Math.Round(discountsSumRateByOrderItem.Key.Price * discountsSumRateByOrderItem.Value, 2)
                });

            var billDiscountInfo = new BillDiscountInfo
            {
                //DiscountAmount = billDiscountItemInfo.Sum(i => i.DiscountAmount),
                Items = billDiscountItemInfo
            };

            return billDiscountInfo;
        }
    }
}
