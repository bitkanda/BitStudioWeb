using bitkanda.Dal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Org.BouncyCastle.Asn1.X509;
using System.Collections.Generic;
using System.Linq;

namespace BitStudioWeb.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductController : Controller
    {
        private readonly MysqlDBContext _dbContext;

        public ProductController(MysqlDBContext dbContext)
        {
            _dbContext = dbContext;
        }
        //[HttpGet("getproducts")]
        //public ActionResult GetProducts(int typeid)
        //{
        //    var data = _dbContext.Products.Where(p => p.TypeId == typeid).ToList(); 
        //    return Json(new { success = true, Result = data });
        //}

       
        [Authorize]
        [HttpPost("addproduct")]
        public ActionResult AddProduct(Product product)
        {
            bool data;
            if (product == null)
            {
                data = false;
            }
            else
            {
                _dbContext.Products.Add(product);
                data = _dbContext.SaveChanges() > 0;
            }
            return Json(new { success = data });
        }

        [Authorize]
        [HttpDelete("delproduct")]
        public ActionResult DelProduct(long id)
        {
            bool data;

            var dbproduct = _dbContext.Products.Find(id);
            if (dbproduct == null)
            {
                data = false;
            }
            else
            {
                _dbContext.Products.Remove(dbproduct);
                data = _dbContext.SaveChanges() > 0;
            }
            return Json(new { success = data });
        }

        [Authorize]
        [HttpPost("updateproduct")]
        public ActionResult UpdateProduct(Product product)
        {
            bool data;
            var dbProduct = _dbContext.Products.Find(product.ID);
            if (dbProduct == null)
            {
                data= false;
            }
            else {
                dbProduct.Price = product.Price;
                dbProduct.Title = product.Title;
                dbProduct.Description = product.Description;
                dbProduct.ImgUrl = product.ImgUrl;
                data = _dbContext.SaveChanges() > 0;
            }
            return Json(new { success = data  });
        }

        [HttpGet("getproducts")]
        public ActionResult GetProducts(long productid)
        {
            var pd = _dbContext.Products.FirstOrDefault(p => p.ID == productid);
            var sk = _dbContext.ProductSkus.Where(p => p.ProductId == productid).ToList();
            var data = new { product = pd, sku = sk };

            return Json(new { success = true, result = data });
        }

        [Authorize]
        [HttpPost("addsku")]
        public ActionResult AddSku(ProductSku productSku)
        {
            bool data;
            if (productSku == null)
            {
                data = false;
            }
            else
            {
                _dbContext.ProductSkus.Add(productSku);
                data = _dbContext.SaveChanges() > 0;
            }
            return Json(new { success = data });
        }

        [Authorize]
        [HttpPost("updatesku")]
        public ActionResult UpdateSku(ProductSku productSku)
        {
            bool data;
            var dbproductSkus = _dbContext.ProductSkus.Find(productSku.ID);
            if (dbproductSkus == null)
            {
                data = false;
            }
            else
            {
                dbproductSkus.Price = productSku.Price;
                dbproductSkus.Name = productSku.Name; 
                data = _dbContext.SaveChanges() > 0;
            }
            return Json(new { success = data });

        }
        [Authorize]
        [HttpDelete("delsku")]
        public ActionResult DelSku(long id)
        {
            bool data;

            var dbproductSkus = _dbContext.ProductSkus.Find(id);
            if (dbproductSkus == null)
            {
                data = false;
            }
            else
            {
                _dbContext.ProductSkus.Remove(dbproductSkus);
                data = _dbContext.SaveChanges() > 0;
            }
            return Json(new { success = data });
        }
    }
}
