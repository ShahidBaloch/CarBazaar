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

            if (!string.IsNullOrEmpty(searchParams.SearchTerm))
            {
                query.Match(Search.Full, searchParams.SearchTerm).SortByTextScore();
            }

            // 1️⃣ Handle sorting (with deterministic tie-breaker)
            query = searchParams.OrderBy switch
            {
                "make" => query.Sort(x => x.Ascending(a => a.Make)),
                "new" => query.Sort(x => x.Descending(a => a.CreatedAt)),
                _ => query.Sort(x => x.Ascending(a => a.AuctionEnd))
            };
 

            if (!string.IsNullOrEmpty(searchParams.SearchTerm))
            {
                query.Match(Search.Full, searchParams.SearchTerm)
                     .SortByTextScore();
            }

            // Optional seller filter
            if (!string.IsNullOrEmpty(searchParams.Seller))
            {
                query.Match(x => x.Seller == searchParams.Seller);
            }

            // Optional winner filter
            if (!string.IsNullOrEmpty(searchParams.Winner))
            {
                query.Match(x => x.Winner == searchParams.Winner);
            }


            query.PageNumber(searchParams.PageNumber > 0 ? searchParams.PageNumber : 1);
            query.PageSize(searchParams.PageSize > 0 ? searchParams.PageSize : 10);

            var result = await query.ExecuteAsync();

            return Ok(new
            {
                results = result.Results,
                pageCount = result.PageCount,
                totalCount = result.TotalCount,
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
