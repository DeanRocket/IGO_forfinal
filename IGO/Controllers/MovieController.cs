﻿using IGO.Models;
using IGO.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IGO.Controllers
{
    public class MovieController : Controller
    {
        private DemoIgoContext _dbIgo;
        public MovieController(DemoIgoContext dbIgo)
        {
            _dbIgo = dbIgo;
        }

        public IActionResult List()
        {
            List<CMovieViewModel> List = new List<CMovieViewModel>();

            //從DB拿出Movie資料
            List<TMovie> dbList = _dbIgo.TMovies.ToList();

            //逐筆轉換成CMovieViewModel格式
            foreach (TMovie t in dbList)
            {
                CMovieViewModel cm = new CMovieViewModel();
                cm.Movie = t;
                var q = _dbIgo.TProductsPhotos.Where(p => p.FMovieId == t.MovieId);
                cm.PhotoPathList = q.Select(x => x.FPhotoPath).ToList();
                List.Add(cm);
                // ---------------------------------------------------------------------
                //List.Add(new CMovieViewModel { Movie = t });
            }

            //List<CMovieViewModel> newList = dbList.Select(t => new CMovieViewModel
            //{
            //    Movie = t
            //}).ToList();

            return View(List);
        }

        public IActionResult Detail(int ID)
        {
            int userid = 0;
            if (HttpContext.Session.Keys.Contains(CDictionary.SK_LOGINED_USER))
            {
                userid = (int)HttpContext.Session.GetInt32(CDictionary.SK_LOGINED_USER);
            }

            var movie = _dbIgo.TMovies.Where(t => t.MovieId == ID).FirstOrDefault();
            var supplier = _dbIgo.TSuppliers.Where(t => t.FCompanyName.Contains("影城")).ToList();
            var showing = _dbIgo.TShowings.ToList();
            var seat = _dbIgo.TMovieSeats.ToList();
            var ticketType = _dbIgo.TMovieTicketTypes.ToList();

            List<CMovieSeatViewModel> List = new List<CMovieSeatViewModel>();

            CMovieViewModel cMovie = new CMovieViewModel();
            cMovie.Movie = movie;
            //cMovie.IsCollection = _dbIgo.TCollections.Where(t => t.FMovieId == ID && t.FCustomerId == userid).Count() > 0;
            cMovie.IsCollection = _dbIgo.TCollections.Any(t => t.FMovieId == ID && t.FCustomerId == userid);
            cMovie.PhotoPathList = _dbIgo.TProductsPhotos.Where(t => t.FMovieId == ID).Select(t => t.FPhotoPath).Skip(1).ToList();

            foreach (TMovieSeat data in seat)
            {
                CMovieSeatViewModel cm = new CMovieSeatViewModel();
                cm.seat = data;
                List.Add(cm);
            }




            return View(new Tuple<CMovieViewModel, List<TSupplier>, List<TShowing>, List<CMovieSeatViewModel>, List<TMovieTicketType>, int>(cMovie, supplier, showing, List, ticketType, userid));
        }

        public JsonResult CancelCollection(int customerID, int movieID)
        {
            //TCollection collection = _dbIgo.TCollections.Where(t => t.FMovieId == movieID && t.FCustomerId == customerID).FirstOrDefault();
            TCollection collection = _dbIgo.TCollections.FirstOrDefault(t => t.FMovieId == movieID && t.FCustomerId == customerID);
            _dbIgo.Remove(collection);
            _dbIgo.SaveChanges();
            return Json(true);
        }

        public JsonResult AddCollection(int customerID, int movieID)
        {
            TCollection collection = new TCollection
            {
                FCustomerId = customerID,
                FMovieId = movieID,
                FCollectionDate = DateTime.Now.ToString()
            };

            _dbIgo.Add(collection);
            _dbIgo.SaveChanges();

            return Json(true);
        }

        public JsonResult SearchChosenSeat(int movieID, string movieDate, int supplierID, int showingID)
        {
            List<TShoppingCart> shoppingCarts = _dbIgo.TShoppingCarts.Where(t => t.FMovieId == movieID &&
                                                                                                                                                          t.FBookingTime == movieDate &&
                                                                                                                                                          t.FSupplierId == supplierID &&
                                                                                                                                                          t.FShowingId == showingID).ToList();

            List<TOrderDetail> orderDetails = _dbIgo.TOrderDetails.Where(t => t.FMovieId == movieID &&
               t.FBookingTime == movieDate && t.FSupplierId == supplierID && t.FShowingId == showingID).ToList();

            List<int> seatIDs = shoppingCarts.Select(t => t.FMovieSeatId ?? 0).ToList();

            //List<int> orderSeatIDs = orderDetails.Select(x => x.FMovieSeatId ?? 0).ToList();
            //seatIDs.AddRange(orderSeatIDs);
            seatIDs.AddRange(orderDetails.Select(x => x.FMovieSeatId ?? 0).ToList());

            List<TMovieSeat> seats = _dbIgo.TMovieSeats.Where(t => seatIDs.Contains(t.FSeatId)).ToList();

            List<string> result = seats.Select(x => x.FSeatRow + x.FSeatColumn).ToList();

            return Json(result);
        }
    }
}