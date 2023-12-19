﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Summary
{
    internal class Day07_Solution : IDaySolution
    {
        public long GetInputSize()
        {
            return Input.Split(Environment.NewLine).Length;
        }

        public long Part1()
        {
            var lines = Input.Split(Environment.NewLine);
            Dictionary<char, byte> cardStrength = new Dictionary<char, byte>()
            {
                {'A', 13 },
                {'K', 12 },
                {'Q', 11 },
                {'J', 10 },
                {'T', 9 },
                {'9', 8 },
                {'8', 7 },
                {'7', 6 },
                {'6', 5 },
                {'5', 4 },
                {'4', 3 },
                {'3', 2 },
                {'2', 1 }
            };
            var cardsByCardStrengthLowestHighest = lines.Select<string, (string cards, int bid)>(line =>
            {
                var t = line.Split(' ');
                return (t[0], int.Parse(t[1]));
            }).OrderBy(line => line.cards, new CardStrengthComparer(cardStrength));
            var orderedCardsLowestHighest = cardsByCardStrengthLowestHighest.OrderBy(c => GetCardsetType(c.cards, false));
            var handWinnigs = orderedCardsLowestHighest.Select((c, i) => c.bid * (i + 1));
            return handWinnigs.Sum();
        }

        public long Part2()
        {
            var lines = Input.Split(Environment.NewLine);
            Dictionary<char, byte> cardStrength = new Dictionary<char, byte>()
            {
                {'A', 13 },
                {'K', 12 },
                {'Q', 11 },
                {'T', 9 },
                {'9', 8 },
                {'8', 7 },
                {'7', 6 },
                {'6', 5 },
                {'5', 4 },
                {'4', 3 },
                {'3', 2 },
                {'2', 1 },
                {'J', 0 }
            };
            var cardsByCardStrengthLowestHighest = lines.Select<string, (string cards, int bid)>(line =>
            {
                var t = line.Split(' ');
                return (t[0], int.Parse(t[1]));
            }).OrderBy(line => line.cards, new CardStrengthComparer(cardStrength));
            var orderedCardsLowestHighest = cardsByCardStrengthLowestHighest.OrderBy(c => GetCardsetType(c.cards, true));
            var handWinnigs = orderedCardsLowestHighest.Select((c, i) => c.bid * (i + 1));
            return handWinnigs.Sum();
        }

        private static byte GetCardsetType(string cards, bool withJoker)
        {
            var t = cards.GroupBy(c => c).ToDictionary((g) => g.Key, g => g.Count());
            var j = 0;
            if (withJoker)
            {
                if (t.ContainsKey('J'))
                {
                    j = t['J'];
                    t.Remove('J');
                }
            }
            var v = t.Values.OrderDescending();
            if (!v.Any())
            {
                return 7;
            }
            else if (v.ElementAt(0) == 5 || v.ElementAt(0) + j == 5)
            {
                return 7;
            }
            else if (v.ElementAt(0) == 4 || v.ElementAt(0) + j == 4)
            {
                return 6;
            }
            else if ((v.ElementAt(0) == 3 || v.ElementAt(0) + j == 3) && v.ElementAt(1) == 2)
            {
                return 5;
            }
            else if (v.ElementAt(0) == 3 || v.ElementAt(0) + j == 3)
            {
                return 4;
            }
            else if ((v.ElementAt(0) == 2 || v.ElementAt(0) + j == 2) && v.ElementAt(1) == 2)
            {
                return 3;
            }
            else if (v.ElementAt(0) == 2 || v.ElementAt(0) + j == 2)
            {
                return 2;
            }
            return 1;
        }

        private class CardStrengthComparer : IComparer<string>
        {
            private readonly Dictionary<char, byte> cardStrength;

            public CardStrengthComparer(Dictionary<char, byte> cardStrength)
            {
                this.cardStrength = cardStrength;
            }

            public int Compare(string? a, string? b)
            {
                if (a == null || b == null) return 0;
                for (int i = 0; i < 5; i++)
                {
                    var charA = a[i];
                    var charB = b[i];
                    if (cardStrength[charA] > cardStrength[charB])
                    {
                        return 1;
                    }
                    else if (cardStrength[charA] < cardStrength[charB])
                    {
                        return -1;
                    }
                }
                return 0;
            }
        }


        private const string Input = """
            QTTQK 749
            JQAA2 148
            37J44 319
            559J5 647
            92992 659
            55AA5 58
            KKTT8 629
            3J38J 562
            87QQQ 434
            55A55 520
            T777T 813
            76T8T 841
            8989A 649
            88Q44 857
            Q4444 362
            T4Q23 369
            AAQQA 812
            34JTQ 635
            QQA44 553
            78787 286
            K963A 63
            27T98 25
            6767A 929
            TT8TK 343
            5566J 496
            A7339 618
            28J8A 641
            Q688J 118
            6JJ46 131
            66436 473
            TK3QK 482
            5454T 724
            TTQQT 74
            ATATA 41
            4523Q 676
            642T4 599
            2A6KQ 203
            J6666 926
            J9999 325
            22232 455
            42444 757
            TTK2T 904
            QQQQ2 879
            6AT32 559
            Q4AT5 456
            66699 106
            AQ248 570
            26676 947
            K4T35 726
            A2KKA 167
            46646 679
            33882 582
            52A66 249
            252K5 293
            84449 920
            J4988 682
            8TTT3 384
            95733 832
            2JKTK 707
            85563 799
            A5A57 359
            77TTA 175
            28222 468
            J2TAK 758
            8T656 833
            9J799 327
            223T8 587
            4K692 512
            Q3JQQ 61
            KQ538 650
            KKKK6 937
            6KK99 532
            7JJ77 810
            3TT33 865
            78J78 710
            A5A54 979
            Q4663 275
            7A837 16
            KKJ5K 102
            AAAA5 753
            T6K65 554
            Q7788 185
            8Q5J4 29
            27672 677
            JQJQQ 983
            57557 6
            KK592 49
            44669 489
            AA99J 56
            T6866 472
            QQ8JJ 542
            9KK99 28
            Q22Q2 893
            K3978 581
            954A5 306
            27377 34
            KKK3J 763
            TT5JQ 708
            Q4796 593
            AAJA6 467
            4TA83 817
            J7JKJ 767
            4TKJJ 671
            Q8KTK 709
            A377A 256
            JJKAA 138
            QKKKK 215
            A8TT7 132
            5JTJ7 798
            888Q8 458
            KKK53 66
            77QAA 99
            69AKA 392
            223J9 745
            AA293 844
            45625 995
            TKKQ6 845
            52555 596
            79977 770
            5A36Q 326
            T239K 822
            33373 276
            7QTKJ 769
            J3334 886
            9J439 23
            39Q93 834
            J6J4A 168
            Q59Q5 492
            73T73 375
            3JQ33 480
            82828 560
            96842 741
            4A5T8 288
            A5A22 540
            99T4T 139
            T2J22 271
            36666 71
            J356K 83
            74478 73
            82J6T 625
            AQQQ3 785
            33J55 446
            7536T 205
            72327 216
            Q33Q3 613
            JJ335 644
            89A99 772
            6JJ66 602
            9AK99 21
            56Q76 594
            Q54J5 981
            4K44K 229
            465KK 692
            54356 648
            2T474 101
            88388 906
            K8747 658
            68226 231
            92292 912
            JT55T 377
            8A75K 962
            Q8898 973
            95559 156
            7858Q 984
            QK2K4 792
            KTK4K 942
            6738T 263
            A322A 470
            72KA5 107
            25525 933
            Q7667 487
            K8668 349
            3A3A5 481
            JJ2J2 459
            3QA23 627
            J4443 622
            5525J 787
            K99KK 222
            54353 410
            AJAA8 860
            99K98 444
            4444J 668
            3799K 110
            A4422 67
            QQTQ5 837
            58825 858
            56K2Q 846
            QQ7QK 145
            26KQT 380
            T8QQQ 820
            6TJTT 358
            28J69 549
            A6Q39 958
            95556 871
            TTQ4K 393
            TQ4K8 105
            23678 95
            3TT83 429
            TTKTT 100
            A333A 177
            58535 129
            J696Q 544
            Q3733 900
            4758K 54
            AAAJJ 226
            88J8K 606
            47K22 609
            3KKKK 253
            48A72 65
            Q6529 233
            J88J8 88
            QAAQ9 423
            99894 576
            6J363 781
            688J6 300
            39333 862
            JK433 488
            484JJ 209
            KKK7K 144
            J6AT9 415
            K46TA 853
            J765K 831
            45495 675
            57K47 206
            JQJQA 563
            AAKA7 885
            54J3K 729
            JTT2T 251
            3J47K 189
            88583 691
            JT6A5 696
            92T84 158
            22336 899
            92578 194
            J9Q99 355
            2J322 891
            555JT 478
            43T43 475
            2929J 416
            6J9JJ 966
            7778A 997
            75T46 283
            8T84Q 701
            A7JA2 19
            5QK5Q 142
            26965 201
            K26K2 528
            4T29Q 605
            89QA5 243
            75QQ5 507
            5855J 133
            689A7 440
            2483T 632
            TT44A 598
            7QQQ5 217
            7AAA7 91
            K2K22 590
            K6K66 603
            4K354 171
            37877 922
            2222J 180
            59522 695
            88247 162
            K79J5 584
            AAJJ3 509
            6993T 24
            33344 277
            J26A9 881
            AATAJ 490
            T8TTT 372
            299TT 212
            K44KK 776
            66778 80
            JAAAA 806
            233QQ 435
            64436 197
            5K555 730
            4T3J4 441
            JJTJT 453
            643T9 438
            9J933 5
            4Q8QJ 294
            55955 978
            22JAQ 350
            T2T4Q 600
            TK986 264
            949J4 971
            TQ7T4 653
            Q33QQ 383
            AA9A8 788
            76464 368
            97J95 221
            6QQQJ 882
            6AA26 267
            T5555 722
            53589 657
            66765 721
            7K28Q 186
            KK3K2 22
            3338K 854
            JK796 451
            A3AJ3 850
            2555T 337
            28A4T 38
            A99AA 536
            JA5A5 421
            JJ444 184
            2JAK9 245
            565J7 718
            75Q38 714
            33444 115
            444Q7 47
            539T5 548
            3333J 597
            T4A6A 989
            47777 916
            Q2645 547
            8AT3Q 237
            QT73T 839
            4AK3J 849
            JJJ8J 329
            Q44QJ 48
            QQ444 951
            6296K 897
            99A9A 699
            69656 140
            55565 821
            Q53QA 495
            83333 333
            3T65T 146
            AA4A4 808
            AAKTA 546
            7JTQ2 713
            TQ3TT 700
            68366 903
            9866K 907
            J36Q5 370
            4KA7J 367
            Q5AQQ 585
            63A6A 330
            6A686 522
            AA566 418
            2TTTT 643
            2TA3A 305
            TTJ99 921
            6K258 218
            33553 181
            KKQKJ 568
            22922 902
            J9KTT 836
            58324 934
            94692 964
            AT574 208
            K63T4 535
            JTT7J 116
            3T6J4 875
            63294 404
            8222J 127
            455J5 15
            8626J 2
            77KKK 51
            84A9J 408
            KJT46 967
            QQQQ9 884
            5Q2QQ 50
            66555 397
            45JT8 460
            KT8K8 232
            75K53 932
            A7678 538
            3383J 927
            2957K 959
            77772 409
            454AK 956
            A362J 228
            TTTT7 382
            8J883 660
            99KT9 639
            KK2J6 557
            84444 356
            99296 430
            5Q5JQ 285
            AAA27 972
            5T847 241
            528QJ 260
            92JA7 361
            35T9T 433
            98TQK 530
            82KJ5 727
            A6A6J 527
            8QQ48 615
            QT656 466
            29225 282
            3388J 407
            4AJ44 965
            TT877 69
            22927 311
            74922 526
            34QQ5 661
            4J844 670
            T8J48 426
            QQQ2T 498
            424J2 656
            AAA8T 616
            4JTT2 172
            43T6T 57
            796K9 321
            TT334 163
            TTT59 579
            5555Q 734
            82432 60
            3A236 257
            55544 794
            53777 230
            27KJ5 946
            952TJ 366
            7333J 502
            KAJAK 476
            88688 268
            689TA 463
            3TTKA 111
            8499T 789
            58J35 178
            7T5K8 395
            4AA3J 428
            AJ746 575
            93339 17
            46TT6 801
            3AAAA 855
            KKK8K 335
            7J777 760
            K77K7 281
            A8599 219
            4Q445 867
            59T6A 11
            K3K35 344
            888K8 454
            65T83 112
            J322A 592
            86666 317
            784A5 940
            63696 55
            8A95K 991
            K6966 736
            88QQK 59
            3J397 82
            4AK52 521
            K54KT 254
            TJ357 948
            QK5KK 130
            8T858 43
            38K5A 662
            33KQK 809
            56T66 72
            47373 743
            TAK24 664
            35T33 856
            AA7AA 44
            88Q33 32
            63636 176
            484A2 800
            44T4K 332
            4QQTQ 52
            J88AA 248
            734QT 278
            J6K88 104
            T2T2T 768
            KKKAQ 715
            T7KJ9 89
            87QAT 347
            3344J 720
            Q88Q8 304
            J8K26 373
            3QJ3K 611
            A8888 608
            63K7K 655
            5TQTQ 239
            7J2QJ 399
            4K2K2 432
            8J233 291
            QJ272 388
            999T4 500
            22Q3Q 457
            3A6QQ 869
            A3QQ3 272
            J8Q89 556
            7KK66 550
            3JKJJ 936
            72JTA 752
            7AQ9T 70
            9Q99Q 974
            Q5Q58 376
            QKTTT 954
            TTTJT 939
            KKKQQ 888
            9T4JQ 250
            AAA65 364
            62334 803
            J7Q29 949
            22J52 619
            84344 316
            746T3 589
            3483J 213
            992T2 159
            K322K 807
            Q7T77 78
            T932Q 960
            JQ67J 134
            6Q644 868
            64J92 30
            JTKTT 150
            96T69 351
            TA6J2 694
            37893 870
            7Q95A 346
            TT6J6 825
            77577 443
            TJQQT 892
            9Q9K8 533
            6Q32A 365
            J297J 652
            6J222 843
            899KK 341
            Q449Q 680
            2T4J4 200
            9J33A 259
            86K2T 174
            42433 246
            88555 155
            9J99J 779
            AA55A 552
            Q427T 442
            JJ222 663
            K9K8K 969
            QQJ87 667
            QJ866 508
            QJ445 588
            4TA74 539
            3TT3T 771
            6766J 980
            5J265 780
            88JJ4 982
            JK664 234
            298JQ 1
            Q664K 452
            6J4QT 737
            9A2A2 740
            88AAA 607
            888T9 119
            98494 994
            557T8 14
            T637J 873
            45555 642
            8J353 621
            6J6K6 985
            2897K 617
            74462 301
            JKQ3Q 151
            6J26K 81
            92462 62
            43557 474
            5Q28K 391
            QAKT2 728
            27JJ7 13
            QQ92Q 419
            AAK9T 796
            395A4 449
            55568 580
            QQTTQ 227
            9K995 595
            5AKKJ 905
            AAQQQ 925
            2TQQT 573
            53553 402
            KKK2K 424
            68KKJ 394
            QJ9A3 523
            99QK9 506
            69269 791
            927K3 379
            6Q5KQ 829
            KKKJK 345
            JK9K9 199
            2AA22 387
            5TK8T 863
            J72Q3 445
            7Q4QT 191
            AAKAJ 970
            4QT5J 381
            A3356 126
            TTATT 141
            32A73 851
            66777 610
            T48JQ 917
            3JQ96 447
            3AK98 626
            74QJ6 187
            33363 531
            Q57J8 634
            J66T6 223
            J3J33 732
            T6TTT 501
            777A3 406
            63779 950
            37777 646
            27A85 160
            TKTKT 911
            A9T24 818
            A7A88 534
            97557 398
            37988 503
            J7776 135
            64J34 202
            76JK2 874
            54584 583
            85KKK 85
            7T34A 830
            65T97 910
            9J6A9 46
            3AQ3J 204
            QA73J 693
            222Q2 315
            J2224 98
            T3T99 968
            2A2AA 883
            2824J 804
            67JA9 420
            3Q7T2 322
            7QQ77 551
            7K364 524
            35274 746
            55T77 566
            22752 612
            83855 688
            J8T55 802
            KKKK9 930
            4T888 686
            TTT23 783
            J7773 665
            63K66 328
            QK2J2 518
            99A33 990
            66A66 269
            2552T 471
            2AK3A 778
            5549Q 759
            T888J 195
            4T4T4 390
            AKAK4 400
            A2QK9 880
            96964 614
            J5454 35
            QQ3QQ 840
            58885 952
            44545 40
            QAQ8T 207
            T722T 262
            AA888 828
            TAK3A 750
            2J5QQ 92
            QQJQQ 169
            J6466 310
            A8867 479
            438Q7 774
            8QQQQ 623
            9T992 773
            J2Q36 887
            Q79Q7 165
            2584A 775
            QT857 918
            AJA9A 620
            76645 895
            44JA3 513
            8TT4T 976
            4TT49 79
            QJQQT 975
            777K7 711
            987TA 637
            29478 352
            44434 996
            KJ9KA 124
            AJ688 411
            4JTT3 748
            33K44 324
            KQJ2K 182
            37J3K 284
            99993 761
            J3393 716
            8558J 986
            T56J3 572
            KATTK 838
            85Q6K 431
            A3636 238
            A8AAA 338
            22599 265
            48248 477
            99599 318
            77999 510
            668TT 386
            8T649 633
            6666Q 913
            97K7K 342
            4Q282 236
            4AAQ8 944
            TQ95J 465
            55854 866
            55666 988
            Q35T2 412
            8J888 45
            2227Q 565
            2K5A6 97
            J94J4 331
            42224 823
            A8AQA 919
            33335 777
            TA98Q 103
            32229 640
            JJJJJ 938
            T22TJ 279
            AA5T9 805
            3335J 705
            T3J62 42
            33222 224
            T44A4 842
            QQQQ6 992
            96K2K 955
            TT6T6 631
            22828 698
            4K295 242
            8QQQJ 469
            3333T 153
            88A9A 96
            8736K 690
            5757Q 638
            9T9A9 137
            JJ555 255
            Q97Q9 287
            25K28 125
            65K6K 681
            253J4 436
            AT2T5 94
            3Q34T 943
            54Q3T 514
            KKQK4 8
            464K4 876
            3KK33 697
            28883 53
            62226 120
            8KK33 628
            QQA98 266
            3K924 824
            222K3 334
            79229 586
            9K999 685
            4Q44K 450
            797J8 963
            666J8 84
            A92J5 751
            999K6 183
            24595 894
            T46J6 504
            TQ2JA 941
            QQ77Q 764
            8QJ3J 928
            7667J 687
            28AT7 307
            52522 462
            9J2JT 340
            TTTJJ 464
            666K5 795
            56QQQ 357
            36J22 782
            42347 68
            72272 957
            96969 378
            TQQ7Q 302
            48422 673
            26662 864
            TKKKT 786
            4768T 26
            366T6 86
            79J97 336
            A4944 739
            6QQKK 961
            82K23 543
            Q9Q93 274
            T6QT3 931
            3KT35 198
            J7T6J 765
            77755 270
            284QK 511
            88668 240
            KKTK6 717
            AAAA2 193
            A4JAA 303
            4AAAA 872
            A78T6 75
            48447 684
            Q222J 354
            6357A 173
            JTK78 122
            QT6Q6 439
            K283J 558
            TJ5JQ 27
            KKK65 567
            J7648 363
            QJ475 702
            J472A 898
            5T3TT 793
            QK5QA 164
            9T97A 9
            KK495 493
            2T2T2 998
            88488 915
            T8777 601
            78888 747
            48478 517
            99898 128
            94996 289
            6Q84A 252
            46J63 114
            43AA3 33
            44TTJ 90
            Q5552 731
            69874 196
            2T228 413
            A282A 896
            49TJ6 108
            4444T 666
            26A2J 313
            22722 371
            65T3K 766
            88798 31
            K78JA 296
            7Q5TT 848
            55K5J 299
            3KK8K 297
            8KA5A 491
            JK6KK 149
            T333Q 290
            TKT7K 577
            2K3A7 414
            6K666 859
            KJ574 591
            63K26 797
            K2KAK 999
            TTT9J 703
            7JJJ2 117
            4JJ92 654
            969J6 624
            J4Q4T 683
            T5TTT 39
            A85AA 645
            7TQQ7 385
            66AA6 403
            9392Q 735
            2KT4Q 190
            95353 486
            58568 516
            739AK 852
            Q5KK2 10
            4K6T4 651
            2Q228 225
            9TTTT 901
            283QJ 136
            QQ3J3 220
            83383 235
            A99A3 485
            AA8AK 401
            J7TTQ 819
            9A772 292
            A6AAA 561
            93TTT 877
            QJ9Q9 147
            47Q77 814
            K638T 425
            AKK8A 571
            T7768 816
            QQ4Q4 497
            63262 738
            727K8 3
            88AT8 353
            K36T7 784
            4K564 12
            4AA44 339
            A2AA8 374
            65J55 312
            88988 188
            69666 309
            AAK5K 192
            77644 993
            82636 273
            5484T 396
            JKKJJ 678
            K3663 574
            TTT99 519
            838K8 515
            JKKJK 755
            2TQ84 494
            55558 258
            ATATT 109
            3TJ3T 719
            TJQAA 37
            66662 914
            58TQ5 64
            5J535 308
            7AQJ8 790
            K8JQT 578
            88885 529
            9K9J9 261
            J5555 247
            3QA86 210
            2479A 157
            JA6TJ 427
            QQ555 733
            AA5Q5 756
            AATAA 564
            8JTJT 762
            85533 725
            K52KK 537
            6A745 890
            8A429 630
            KK888 826
            T4T4T 280
            Q3AAT 669
            32958 93
            A342T 121
            K2KJK 320
            3K333 754
            6TTAA 636
            T4TTT 742
            AKAAK 389
            8JK8Q 545
            A7777 123
            T6282 815
            5Q42T 847
            99969 323
            733TJ 244
            46589 604
            J8KKK 166
            844K9 908
            Q3K56 20
            JA446 689
            5QQ7K 448
            4T927 7
            72474 211
            222T2 977
            88989 4
            8Q828 525
            2TT52 113
            36536 835
            A2JAA 923
            Q7983 295
            T6534 1000
            96939 461
            598K2 298
            QKQAA 154
            AAKAA 861
            TQ696 674
            K3Q9A 348
            QQQQ4 827
            88K84 987
            KK555 712
            22777 889
            254T4 672
            3K23K 483
            44463 811
            Q77AQ 878
            AK5KK 555
            AQAAA 214
            QA7K9 484
            TT5KK 161
            T7QQT 541
            AK6A6 422
            66967 314
            7KQ79 437
            77677 704
            T76JQ 505
            A525J 143
            83533 360
            J4Q53 36
            2K7AJ 170
            Q4AAA 87
            47774 953
            TK2KQ 935
            K67T4 909
            T8Q43 924
            33995 945
            JQA8T 569
            KKK55 744
            68866 152
            2J2TK 18
            Q7QQQ 417
            T7Q82 76
            53322 405
            TA4Q3 723
            2Q9J9 499
            36336 179
            9Q494 706
            87JK6 77
            """;
    }
}
