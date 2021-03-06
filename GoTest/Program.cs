﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Go;

namespace GoTest
{
    class Program
    {
        static shared_strand _strand;
        static chan<long> _chan1;
        static chan<long> _chan2;
        static chan<long> _chan3;
        static csp_chan<long, long> _csp;

        static async Task Producer1()
        {
            while (true)
            {
                await _chan1.send(system_tick.get_tick_us());
                await generator.sleep(300);
            }
        }

        static async Task Producer2()
        {
            while (true)
            {
                await _chan2.receive();
                await generator.sleep(500);
            }
        }

        static async Task Producer3()
        {
            while (true)
            {
                await _chan3.send(system_tick.get_tick_us());
                await generator.sleep(1000);
            }
        }

        static async Task Producer4()
        {
            while (true)
            {
                long res = await _csp.invoke(system_tick.get_tick_us());
                Console.WriteLine("csp return {0}", res);
                await generator.sleep(1000);
            }
        }

        static async Task Consumer()
        {
            Console.WriteLine("receive chan1 {0}", await _chan1.receive());
            Console.WriteLine("send chan2 {0}", await _chan2.send(system_tick.get_tick_us()));
            Console.WriteLine("receive chan3 {0}", await _chan3.receive());
            while (true)
            {
                await generator.select().case_receive(_chan1, async delegate (long msg)
                {
                    Console.WriteLine("select receive chan1 {0}", msg);
                    await generator.sleep(100);
                }).case_send(_chan2, system_tick.get_tick_us(), async delegate ()
                {
                    Console.WriteLine("select send chan2");
                    await generator.sleep(100);
                }).case_receive(_chan3, async delegate (long msg)
                {
                    Console.WriteLine("select receive chan3 {0}", msg);
                    await generator.sleep(100);
                }).case_receive(_csp, async delegate (long msg)
                {
                    Console.WriteLine("select csp delay {0}", system_tick.get_tick_us() - msg);
                    await generator.sleep(100);
                    return system_tick.get_tick_us();
                }).end();
            }
        }

        static async Task Producer5(generator cons)
        {
            for (int i = 0; i < 10; i++)
            {
                await cons.send_msg(i);
                await generator.sleep(1000);
                await cons.send_msg((long)i);
                await generator.sleep(1000);
            }
        }

        static async Task Consumer2()
        {
            await generator.receive().case_of(async delegate (int msg)
            {
                Console.WriteLine("                                   receive int {0}", msg);
                await generator.sleep(1);
            }).case_of(async delegate (long msg)
            {
                Console.WriteLine("                                   receive long {0}", msg);
                await generator.sleep(1);
            }).end();
        }

        static void Main(string[] args)
        {
            work_service work = new work_service();
            _strand = new work_strand(work);
            _chan1 = chan<long>.make(_strand, 3);
            _chan2 = chan<long>.make(_strand, 0);
            _chan3 = chan<long>.make(_strand, -1);
            _csp = new csp_chan<long, long>(_strand);
            generator.go(_strand, Producer1);
            generator.go(_strand, Producer2);
            generator.go(_strand, Producer3);
            generator.go(_strand, Producer4);
            generator.go(_strand, Consumer);
            generator.go(_strand, () => Producer5(generator.tgo(_strand, Consumer2)));
            work.run();
        }
    }
}
