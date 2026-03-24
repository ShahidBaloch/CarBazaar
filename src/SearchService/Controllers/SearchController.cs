using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using MongoDB.Entities;
using SearchService.Models;
using SearchService.RequestHelpers;

namespace SearchService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SearchController : ControllerBase
    {
        public async Task<ActionResult> SearchItems([FromQuery] SearchParams searchParams)
        {
            var query = DB.Instance().PagedSearch<Item, Item>();

            // 1. MUST MATCH FIRST
            bool isFullTextSearch = !string.IsNullOrEmpty(searchParams.SearchTerm);
            if (isFullTextSearch)
            {
                query.Match(Search.Full, searchParams.SearchTerm);
            }

            // 2. FILTERS
            if (!string.IsNullOrEmpty(searchParams.Seller))
                query.Match(x => x.Seller == searchParams.Seller);

            if (!string.IsNullOrEmpty(searchParams.Winner))
                query.Match(x => x.Winner == searchParams.Winner);

            // 3. SORTING (The Fix)
            // Only call SortByTextScore if a Match(Search.Full) actually happened.
            if (isFullTextSearch && string.IsNullOrEmpty(searchParams.OrderBy))
            {
                query.SortByTextScore();
            }
            else
            {
                query = searchParams.OrderBy switch
                {
                    "make" => query.Sort(x => x.Ascending(a => a.Make)),
                    "new" => query.Sort(x => x.Descending(a => a.CreatedAt)),
                    _ => query.Sort(x => x.Ascending(a => a.AuctionEnd))
                };
            }

            query.PageNumber(searchParams.PageNumber > 0 ? searchParams.PageNumber : 1);
            query.PageSize(searchParams.PageSize > 0 ? searchParams.PageSize : 4);

            var result = await query.ExecuteAsync();

            return Ok(new
            {
                results = result.Results,
                pageCount = result.PageCount,
                totalCount = result.TotalCount
            });
        }


        //[HttpGet]
        //public async Task<ActionResult<List<Item>>> SearchItems(string? searchParams)
        //{
        //    // Fix: PagedSearch is an instance method on DB, call via DB.Instance()
        //    var query = DB.Instance().Find<Item>();

        //    query.Sort(x => x.Ascending(a => a.Make));
        //    var result = await query.ExecuteAsync();
        //    return result;
        //}
    }
}
